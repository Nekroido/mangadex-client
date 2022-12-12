namespace Mangadexter.Core

module Preferences =
   type LoadPreferences = unit -> Async<Result<Preferences, exn>>
   type UpdatePreferences = Preferences -> Async<Result<unit, exn>>
