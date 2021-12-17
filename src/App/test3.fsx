open System

type QueueItem =
    { Name: string
      Expression: Async<unit> }

type QueueState =
    { CompletedItems: QueueItem list
      FailedItems: QueueItem list }

type Payload =
    | Job of QueueItem
    | State of AsyncReplyChannel<QueueState>

type QueueWorker() =
    static let updateState state item =
        try
            item.Expression |> Async.RunSynchronously

            { state with
                  CompletedItems = item :: state.CompletedItems }
        with
        | ex ->
            { state with
                  FailedItems = item :: state.FailedItems }

    static let worker =
        MailboxProcessor.Start
            (fun inbox ->
                let rec workerLoop state =
                    async {
                        let! payload = inbox.Receive()

                        let newState =
                            payload
                            |> function
                                | Payload.Job item -> updateState state item
                                | Payload.State (reply) ->
                                    reply.Reply(state)
                                    state

                        return! workerLoop newState
                    }

                workerLoop
                    { CompletedItems = []
                      FailedItems = [] })

    static member Enqueue item = item |> Payload.Job |> worker.Post

    static member State() =
        worker.PostAndReply(fun state -> Payload.State(state))

let rand = Random()

let makeItem () =
    { Name = $"Item #{rand.Next(0, 100)}"
      Expression =
          async {
              let ms = rand.Next(100, 300)
              printfn $"Working for %d{ms}"
              do! Async.Sleep ms
          } }

let makeFailingItem () =
    { Name = $"Item #{rand.Next(0, 100)}"
      Expression =
          async {
              printf "I will fail after 3s"
              do! Async.Sleep 3000
              failwith "I failed!"
          } }

Seq.init 3 (fun _ -> makeFailingItem ())
|> Seq.iter QueueWorker.Enqueue

Seq.init 15 (fun _ -> makeItem ())
|> Seq.iter QueueWorker.Enqueue
