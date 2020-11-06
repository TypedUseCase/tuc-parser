namespace Tuc

open Tuc.Domain

[<RequireQualifiedAccess>]
type KeyWord =
    | TucName
    | Participants
    | Alias
    | Section
    | Group
    | If
    | Else
    | Loop

[<RequireQualifiedAccess>]
module KeyWord =
    let parse = function
        | "tuc" -> KeyWord.TucName
        | "participants" -> KeyWord.Participants
        | "as" -> KeyWord.Alias
        | "section" -> KeyWord.Section
        | "group" -> KeyWord.Group
        | "if" -> KeyWord.If
        | "else" -> KeyWord.Else
        | "loop" -> KeyWord.Loop
        | undefined -> failwithf "KeyWord %A is not defined." undefined

    let value = function
        | KeyWord.TucName -> "tuc"
        | KeyWord.Participants -> "participants"
        | KeyWord.Alias -> "as"
        | KeyWord.Section -> "section"
        | KeyWord.Group -> "group"
        | KeyWord.If -> "if"
        | KeyWord.Else -> "else"
        | KeyWord.Loop -> "loop"

    let descriptionMarkDown = function
        | KeyWord.TucName -> "This is a start of a **tuc** definition. (_It will be a section in puml result._)"
        | KeyWord.Participants -> "A participants keyword starts a participants definition section, where all use-case participants **must** be defined."
        | KeyWord.Alias -> "Used to give the current participant an alias, which will be shown in the result."
        | KeyWord.Section -> "It is a simple divider in the puml."
        | KeyWord.Group -> "Allows to group use-case parts together."
        | KeyWord.If -> "Allows to group use-case parts together by a condition."
        | KeyWord.Else -> "Allows to group use-case parts together, when a condition does not pass."
        | KeyWord.Loop -> "Allows to group use-case parts together in a loop with a condition."

type TucName = TucName of string

[<RequireQualifiedAccess>]
module TucName =
    let value (TucName name) = name

[<RequireQualifiedAccess>]
type DataError =
    | Empty
    | WrongFormat

[<RequireQualifiedAccess>]
type EventError =
    | Empty
    | WrongFormat

type Data = {
    Original: string
    Path: string list
}

[<RequireQualifiedAccess>]
module Data =
    let ofString = function
        | String.IsEmpty ->
            Error DataError.Empty
        | wrongFormat when wrongFormat.Contains " " || wrongFormat.StartsWith "." || wrongFormat.EndsWith "." ->
            Error DataError.WrongFormat
        | event ->
            Ok {
                Original = event
                Path = event.Split "." |> List.ofSeq
            }

    let path { Path = path } = path

    let lastInPath =
        path
        >> List.rev
        >> List.head

    let value = path >> String.concat "."

    // @see https://plantuml.com/link
    let link = function
        | { Path = [ single ] } -> single
        | { Path = _ } as event -> sprintf "[[{%s}%s]]" (event |> value) (event |> lastInPath)

type Event = Event of Data

[<RequireQualifiedAccess>]
module Event =
    open ErrorHandling.Result.Operators

    let data (Event data) = data

    let ofString = Data.ofString >!> Event >@> (function
        | DataError.Empty -> EventError.Empty
        | DataError.WrongFormat -> EventError.WrongFormat
    )
    let path = data >> Data.path
    let lastInPath = data >> Data.lastInPath
    let value = data >> Data.value
    let link = data >> Data.link

type Tuc = {
    Name: TucName
    Participants: Participant list
    Parts: TucPart list
}

and Participant =
    | Component of ParticipantComponent
    | Participant of ActiveParticipant

and ParticipantComponent = {
    Name: string
    Participants: ActiveParticipant list
}

and ActiveParticipant =
    | Service of ServiceParticipant
    | DataObject of DataObjectParticipant
    | Stream of StreamParticipant

and ServiceParticipant = {
    Domain: DomainName
    Context: string
    Alias: string
    ServiceType: DomainType
}

and DataObjectParticipant = {
    Domain: DomainName
    Context: string
    Alias: string
    DataObjectType: DomainType
}

and StreamParticipant = {
    Domain: DomainName
    Context: string
    Alias: string
    StreamType: DomainType
}

and TucPart =
    | Section of Section
    | Group of Group
    | If of If
    | Loop of Loop
    | Lifeline of Lifeline
    | ServiceMethodCall of ServiceMethodCall
    | PostData of PostData
    | ReadData of ReadData
    | PostEvent of PostEvent
    | ReadEvent of ReadEvent
    | HandleEventInStream of HandleEventInStream
    | Do of Do
    | LeftNote of Note
    | Note of CallerNote
    | RightNote of Note

and Section = {
    Value: string
}

and Group = {
    Name: string
    Body: TucPart list
}

and Loop = {
    Condition: string
    Body: TucPart list
}

and If = {
    Condition: string
    Body: TucPart list
    Else: (TucPart list) option
}

and Lifeline = {
    Initiator: ActiveParticipant
    Execution: TucPart list
}

and ServiceMethodCall = {
    Caller: ActiveParticipant
    Service: ActiveParticipant
    Method: MethodDefinition
    Execution: TucPart list
}

and PostData = {
    Caller: ActiveParticipant
    DataObject: ActiveParticipant
    Data: Data
}

and ReadData = {
    Caller: ActiveParticipant
    DataObject: ActiveParticipant
    Data: Data
}

and PostEvent = {
    Caller: ActiveParticipant
    Stream: ActiveParticipant
    Event: Event
}

and ReadEvent = {
    Caller: ActiveParticipant
    Stream: ActiveParticipant
    Event: Event
}

and HandleEventInStream = {
    Stream: ActiveParticipant
    Service: ActiveParticipant
    Handler: HandlerMethodDefinition
    Execution: TucPart list
}

and Do = {
    Caller: ActiveParticipant
    Actions: string list
}

and CallerNote = {
    Caller: ActiveParticipant
    Lines: string list
}

and Note = {
    Lines: string list
}

[<RequireQualifiedAccess>]
module Tuc =
    let name ({ Name = name }: Tuc) = name

[<RequireQualifiedAccess>]
module Participant =
    let active = function
        | Component { Participants = participants } -> participants
        | Participant participant -> [ participant ]

[<RequireQualifiedAccess>]
module ActiveParticipant =
    let name = function
        | Service { ServiceType = t }
        | DataObject { DataObjectType = t }
        | Stream { StreamType = t } -> t |> DomainType.nameValue

    let value = function
        | Service { ServiceType = t } -> t |> DomainType.nameValue
        | DataObject { DataObjectType = t }
        | Stream { StreamType = t } -> t |> DomainType.nameValue |> sprintf "[%s]"
