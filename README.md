Tuc Parser
==========

[![NuGet Version and Downloads count](https://buildstats.info/nuget/Tuc.Parser)](https://www.nuget.org/packages/Tuc.Parser)
![Check](https://github.com/TypedUseCase/tuc-parser/workflows/Check/badge.svg)
[![tuc-docs](https://img.shields.io/badge/documentation-tuc-orange.svg)](https://typedusecase.github.io/domain/)

> A parser for `TUC` files.

## Example

The process of this use-case is a collecting interactions by the users.

User interacts with the GenericService, which sends an interaction to the interaction collector service.
Interaction collector service identify a person and accepts an interaction.

*It is just a simplified real-life process.*

*Note: All files are in the [example](https://github.com/TypedUseCase/tuc-console/tree/master/example) dir.*

### consentsDomain.fsx
```fs
// Common types

type Id = UUID

type Stream<'Event> = Stream of 'Event list
type StreamHandler<'Event> = StreamHandler of ('Event -> unit)

// Types

type InteractionEvent =
    | Confirmation
    | Rejection

type InteractionResult =
    | Accepted
    | Error

type IdentityMatchingSet = {
    Contact: Contact
}

and Contact = {
    Email: Email option
    Phone: Phone option
}

and Email = Email of string
and Phone = Phone of string

type Person =
    | Known of PersonId
    | Unknown

and PersonId = PersonId of Id

// Streams

type InteractionCollectorStream = InteractionCollectorStream of Stream<InteractionEvent>

// Services

type GenericService = Initiator

type InteractionCollector = {
    PostInteraction: InteractionEvent -> InteractionResult
}

type PersonIdentificationEngine = {
    OnInteractionEvent: StreamHandler<InteractionEvent>
}

type PersonAggregate = {
    IdentifyPerson: IdentityMatchingSet -> Person
}

type ConsentManager = {
    GenericService: GenericService
    InteractionCollector: InteractionCollector
}
```

### definition.tuc
```tuc
tuc Identify person on interaction
participants
  ConsentManager consents
    GenericService as "Generic Service"
    InteractionCollector consents as "Interaction Collector"
  [InteractionCollectorStream] consents
  PersonIdentificationEngine consents as "PID"
  PersonAggregate consents

GenericService
  InteractionCollector.PostInteraction
    do create an interaction event based on interaction
    InteractionEvent -> [InteractionCollectorStream]

    [InteractionCollectorStream]
      PersonIdentificationEngine.OnInteractionEvent
        PersonAggregate.IdentifyPerson
          do
            normalize contact
            identify a person based on the normalized contact

          if PersonFound
            do return Person
          else
            do return Error
```

### Parsed result
```
Tuc: Identify person on interaction
 - example/definition.tuc #000 at 000 -> 003: tuc [3]  // KeyWord
 - example/definition.tuc #000 at 004 -> 034: Identify person on interaction [30]  // Value

participants
 - example/definition.tuc #001 at 000 -> 012: participants [12]  // KeyWord


ConsentManager:
    Generic Service(Consents.GenericService)
    Interaction Collector(Consents.InteractionCollector)
 - example/definition.tuc #002 at 002 -> 016: ConsentManager [14]  // Context
 - example/definition.tuc #002 at 017 -> 025: Consents [8]  // Domain

 - example/definition.tuc #003 at 004 -> 018: GenericService [14]  // Context
 - example/definition.tuc #003 at 023 -> 038: Generic Service [15]  // Alias

 - example/definition.tuc #004 at 004 -> 024: InteractionCollector [20]  // Context
 - example/definition.tuc #004 at 025 -> 033: Consents [8]  // Domain
 - example/definition.tuc #004 at 038 -> 059: Interaction Collector [21]  // Alias

[InteractionCollectorStream(Consents.InteractionCollectorStream)]
 - example/definition.tuc #005 at 002 -> 030: [InteractionCollectorStream] [28]  // Context
 - example/definition.tuc #005 at 031 -> 039: Consents [8]  // Domain

PID(Consents.PersonIdentificationEngine)
 - example/definition.tuc #006 at 002 -> 028: PersonIdentificationEngine [26]  // Context
 - example/definition.tuc #006 at 029 -> 037: Consents [8]  // Domain
 - example/definition.tuc #006 at 042 -> 045: PID [3]  // Alias

PersonAggregate(Consents.PersonAggregate)
 - example/definition.tuc #007 at 002 -> 017: PersonAggregate [15]  // Context
 - example/definition.tuc #007 at 018 -> 026: Consents [8]  // Domain


Generic Service(Consents.GenericService)  // lifeline
    -> Interaction Collector(Consents.InteractionCollector).PostInteraction()  // Called by Generic Service(Consents.GenericService)
        Do: create an interaction event based on interaction
        post: InteractionEvent -> [InteractionCollectorStream(Consents.InteractionCollectorStream)]  // Called by Interaction Collector(Consents.InteractionCollector)
        [InteractionCollectorStream(Consents.InteractionCollectorStream)]
            PID(Consents.PersonIdentificationEngine).OnInteractionEvent()
            -> PersonAggregate(Consents.PersonAggregate).IdentifyPerson()  // Called by PID(Consents.PersonIdentificationEngine)
                Do:
                    normalize contact
                    identify a person based on the normalized contact
                if PersonFound
                    Do: return Person
                else
                    Do: return Error
            <- Person
    <- InteractionResult
 - example/definition.tuc #009 at 000 -> 014: GenericService [14]  // Lifeline
-> Interaction Collector(Consents.InteractionCollector).PostInteraction()  // Called by Generic Service(Consents.GenericService)
    Do: create an interaction event based on interaction
    post: InteractionEvent -> [InteractionCollectorStream(Consents.InteractionCollectorStream)]  // Called by Interaction Collector(Consents.InteractionCollector)
    [InteractionCollectorStream(Consents.InteractionCollectorStream)]
        PID(Consents.PersonIdentificationEngine).OnInteractionEvent()
        -> PersonAggregate(Consents.PersonAggregate).IdentifyPerson()  // Called by PID(Consents.PersonIdentificationEngine)
            Do:
                normalize contact
                identify a person based on the normalized contact
            if PersonFound
                Do: return Person
            else
                Do: return Error
        <- Person
<- InteractionResult
 - example/definition.tuc #010 at 002 -> 023: InteractionCollector. [21]  // Service
 - example/definition.tuc #010 at 023 -> 038: PostInteraction [15]  // Method
Do: create an interaction event based on interaction
post: InteractionEvent -> [InteractionCollectorStream(Consents.InteractionCollectorStream)]  // Called by Interaction Collector(Consents.InteractionCollector)
 - example/definition.tuc #012 at 004 -> 020: InteractionEvent [16]  // Data
 - example/definition.tuc #012 at 021 -> 023: -> [2]  // Operator
 - example/definition.tuc #012 at 024 -> 052: [InteractionCollectorStream] [28]  // DataObject
[InteractionCollectorStream(Consents.InteractionCollectorStream)]
    PID(Consents.PersonIdentificationEngine).OnInteractionEvent()
    -> PersonAggregate(Consents.PersonAggregate).IdentifyPerson()  // Called by PID(Consents.PersonIdentificationEngine)
        Do:
            normalize contact
            identify a person based on the normalized contact
        if PersonFound
            Do: return Person
        else
            Do: return Error
    <- Person
 - example/definition.tuc #014 at 004 -> 032: [InteractionCollectorStream] [28]  // Stream
 - example/definition.tuc #015 at 006 -> 033: PersonIdentificationEngine. [27]  // Service
 - example/definition.tuc #015 at 033 -> 051: OnInteractionEvent [18]  // Method
-> PersonAggregate(Consents.PersonAggregate).IdentifyPerson()  // Called by PID(Consents.PersonIdentificationEngine)
    Do:
        normalize contact
        identify a person based on the normalized contact
    if PersonFound
        Do: return Person
    else
        Do: return Error
<- Person
 - example/definition.tuc #016 at 008 -> 024: PersonAggregate. [16]  // Service
 - example/definition.tuc #016 at 024 -> 038: IdentifyPerson [14]  // Method
Do:
    normalize contact
    identify a person based on the normalized contact
if PersonFound
    Do: return Person
else
    Do: return Error
 - example/definition.tuc #021 at 010 -> 012: if [2]  // KeyWord
 - example/definition.tuc #021 at 013 -> 024: PersonFound [11]  // Condition
Do: return Person
 - example/definition.tuc #023 at 010 -> 014: else [4]  // KeyWord
Do: return Error
```

---
### Development

First run:
```shell
./build.sh  # or build.cmd if your OS is Windows  (might need ./build Build here)
```

Everything is done via `build.cmd` \ `build.sh` (_for later on, lets call it just `build`_).
- to run a specific target use `build -t <target>`
