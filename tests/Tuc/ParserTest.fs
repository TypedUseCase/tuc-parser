module Tuc.Parser.Test.Parser

open Expecto
open System.IO

open Tuc
open Tuc.Domain

[<AutoOpen>]
module Common =
    open Tuc.Parser

    let (</>) a b = Path.Combine(a, b)

    let expectFile expected actualLines description =
        Expect.isTrue (expected |> File.Exists) description

        let expectedLines = expected |> File.ReadAllLines |> List.ofSeq
        let actualLines = actualLines |> List.ofSeq

        let separator = String.replicate 50 "."

        Expect.equal
            (actualLines |> List.length)
            (expectedLines |> List.length)
            (sprintf "%s\nActual:\n%s\n%s\n%s"
                description
                separator
                (actualLines |> List.mapi (fun i line -> sprintf "% 3i| %s" i line) |> String.concat "\n")
                separator
            )

        expectedLines
        |> List.iteri (fun i expectedLine ->
            Expect.equal actualLines.[i] expectedLine (sprintf "%s - error at line: #%d" description i)
        )

    let orFail formatError = function
        | Ok ok -> ok
        | Error error -> error |> formatError |> failtestf "%s"

    type Case = {
        Description: string
        Tuc: string
        Expected: Result<ParsedTuc list, Tuc.ParseError list>
    }

    let case path description tuc expected =
        {
            Description = description
            Tuc = path </> tuc
            Expected = expected
        }

    let test output domainTypes { Tuc = tuc; Expected = expected; Description = description } =
        let parsedTucs =
            tuc
            |> Parser.parse output false domainTypes

        match expected, parsedTucs with
        | Ok [], Ok actual ->
            // todo - this is a special case, for now, so the parsing could be checked at least for a Ok result, because the type "tree" is so big
            Expect.isFalse (actual |> List.isEmpty) description

        | Ok expected, Ok actual ->
            Expect.equal expected actual description

        | Error expected, Error actual -> Expect.equal actual expected description
        | Error _, Ok success -> failtestf "%s - Error was expected, but it results in ok.\n%A" description success
        | Ok _, Error error -> failtestf "%s - Success was expected, but it results in error.\n%A" description error

module Domain =
    open ErrorHandling

    let private parseDomain output domain =
        result {
            let! resolvedTypes =
                domain
                |> Parser.parse output
                |> Resolver.resolveOneAsync output
                |> Async.RunSynchronously
                |> Result.mapError (function
                    | AsyncResolveError.UnresolvedTypes types -> types
                    | _ -> []
                )

            return! resolvedTypes |> Checker.check output
        }
        |> orFail (List.map TypeName.value >> String.concat "\n  - " >> sprintf "Unresolved types:\n%s")

    let parseDomainsTypes output path domainFiles =
        let addPath domainFile = path </> domainFile

        domainFiles
        |> List.collect (addPath >> (parseDomain output))

[<RequireQualifiedAccess>]
module Parts =
    let path = "./Tuc/Fixtures/parts"

    let case = case path

    let provider: Case list =
        let genericService = Service {
            Domain = DomainName.create "consents"
            Context = "GenericService"
            Alias = "Generic Service"
            ServiceType =
                DomainType (
                    SingleCaseUnion {
                        Domain = Some (DomainName.create "consents")
                        Name = TypeName "GenericService"
                        ConstructorName = "Initiator"
                        ConstructorArgument = Type (TypeName "unit")
                    }
                )
        }

        [
            case "Sections" "section.tuc" (Ok [
                {
                    Name = Parsed.KeyWord {
                        Value = TucName "Section test"
                        KeyWord = KeyWord.TucName
                        KeyWordLocation = {
                            Value = "tuc"
                            Location = {
                                Uri = "./Tuc/Fixtures/parts/section.tuc"
                                Range = {
                                    Start = { Line = 0; Character = 0 }
                                    End = { Line = 0; Character = 3 }
                                }
                            }
                        }
                        ValueLocation = {
                            Value = "Section test"
                            Location = {
                                Uri = "./Tuc/Fixtures/parts/section.tuc"
                                Range = {
                                    Start = { Line = 0; Character = 4 }
                                    End = { Line = 0; Character = 16 }
                                }
                            }
                        }
                    }

                    ParticipantsKeyWord = Parsed.KeyWordWithoutValue {
                        KeyWord = KeyWord.Participants
                        KeyWordLocation = {
                            Value = "participants"
                            Location = {
                                Uri = "./Tuc/Fixtures/parts/section.tuc"
                                Range = {
                                    Start = { Line = 1; Character = 0 }
                                    End = { Line = 1; Character = 12 }
                                }
                            }
                        }
                    }

                    Participants = [
                        Parsed.ParticipantDefinition {
                            Value =
                                Participant (
                                    Service {
                                        Domain = DomainName.create "parts"
                                        Context = "GenericService"
                                        Alias = "GenericService"
                                        ServiceType =
                                            DomainType (
                                                SingleCaseUnion {
                                                    Domain = Some (DomainName.create "Parts")
                                                    Name = TypeName "GenericService"
                                                    ConstructorName = "Initiator"
                                                    ConstructorArgument = Type (TypeName "unit")
                                                }
                                            )
                                    }
                                )
                            Context = {
                                Value = "GenericService"
                                Location = {
                                    Uri = "./Tuc/Fixtures/parts/section.tuc"
                                    Range = {
                                        Start = { Line = 2; Character = 4 }
                                        End = { Line = 2; Character = 18 }
                                    }
                                }
                            }
                            Domain = Some {
                                Value = "Parts"
                                Location = {
                                    Uri = "./Tuc/Fixtures/parts/section.tuc"
                                    Range = {
                                        Start = { Line = 2; Character = 19 }
                                        End = { Line = 2; Character = 24 }
                                    }
                                }
                            }
                            Alias = None
                            Component = None
                        }
                    ]

                    Parts = [
                        Parsed.KeyWord {
                            Value = Section { Value = "One" }
                            KeyWord = KeyWord.Section
                            KeyWordLocation = {
                                Value = "section"
                                Location = {
                                    Uri = "./Tuc/Fixtures/parts/section.tuc"
                                    Range = {
                                        Start = { Line = 4; Character = 0 }
                                        End = { Line = 4; Character = 7 }
                                    }
                                }
                            }
                            ValueLocation = {
                                Value = "One"
                                Location = {
                                    Uri = "./Tuc/Fixtures/parts/section.tuc"
                                    Range = {
                                        Start = { Line = 4; Character = 8 }
                                        End = { Line = 4; Character = 11 }
                                    }
                                }
                            }
                        }
                        Parsed.KeyWord {
                            Value = Section { Value = "Two words" }
                            KeyWord = KeyWord.Section
                            KeyWordLocation = {
                                Value = "section"
                                Location = {
                                    Uri = "./Tuc/Fixtures/parts/section.tuc"
                                    Range = {
                                        Start = { Line = 6; Character = 0 }
                                        End = { Line = 6; Character = 7 }
                                    }
                                }
                            }
                            ValueLocation = {
                                Value = "Two words"
                                Location = {
                                    Uri = "./Tuc/Fixtures/parts/section.tuc"
                                    Range = {
                                        Start = { Line = 6; Character = 8 }
                                        End = { Line = 6; Character = 17 }
                                    }
                                }
                            }
                        }
                        Parsed.KeyWord {
                            Value = Section { Value = "More words with 42" }
                            KeyWord = KeyWord.Section
                            KeyWordLocation = {
                                Value = "section"
                                Location = {
                                    Uri = "./Tuc/Fixtures/parts/section.tuc"
                                    Range = {
                                        Start = { Line = 8; Character = 0 }
                                        End = { Line = 8; Character = 7 }
                                    }
                                }
                            }
                            ValueLocation = {
                                Value = "More words with 42"
                                Location = {
                                    Uri = "./Tuc/Fixtures/parts/section.tuc"
                                    Range = {
                                        Start = { Line = 8; Character = 8 }
                                        End = { Line = 8; Character = 26 }
                                    }
                                }
                            }
                        }
                    ]
                }
            ])

            case "Initiator's lifeline" "lifeline.tuc" (Ok [])

            case "Notes" "note.tuc" (Ok [])
            case "Note without a caller" "note-without-caller.tuc" (Error [ NoteWithoutACaller (5, 0, @"""Note without caller""") ])

            case "Do" "do.tuc" (Ok [])
            case "Do without a caller" "do-without-caller.tuc" (Error [ DoWithoutACaller (5, 0, "do Some stuff") ])

            case "Handle event in stream" "handle-event-in-stream.tuc" (Ok [])

            //case "Read data from data object" "read-data.tuc" (Ok [])
            //case "Post data to data object" "post-data.tuc" (Ok [])

            case "Read event from stream" "read-event.tuc" (Ok [])
            case "Post event to stream" "post-event.tuc" (Ok [])

            case "Call a service method" "service-method-call.tuc" (Ok [])

            case "Loops" "loop.tuc" (Ok [])
            case "Groups" "group.tuc" (Ok [])
            case "If" "if.tuc" (Ok [])
        ]

[<RequireQualifiedAccess>]
module ParseErrors =
    let path = "./Tuc/Fixtures/errors"

    let case = case path

    let provider: Case list =
        [
            // Tuc file
            case "MissingTucName" "MissingTucName.tuc" (Error [ MissingTucName ])
            case "TucMustHaveName" "TucMustHaveName.tuc" (Error [ TucMustHaveName (1, 0, "tuc") ])
            case "MissingParticipants"  "MissingParticipants.tuc"  (Error [ MissingParticipants ])
            case "MissingIndentation" "MissingIndentation.tuc" (Error [ MissingIndentation ])
            case "WrongIndentationLevel" "WrongIndentationLevel.tuc" (Error [ WrongIndentationLevel (4, [
                "  6|    do nothing"
                "  8|  \"Also here\""
                " 12|      \"> And here is it wrong\""
            ]) ])
            case "TooMuchIndented" "TooMuchIndented.tuc" (Error [ TooMuchIndented (6, 4, "        \"This is too much indented\"") ])

            // Participants
            case "WrongParticipantIndentation" "WrongParticipantIndentation.tuc" (Error [ WrongParticipantIndentation (4, 4, "        StreamListener parts") ])
            case "WrongParticipantIndentation in component" "WrongParticipantIndentation-in-component.tuc" (Error [ WrongParticipantIndentation (4, 8, "            StreamListener parts") ])
            case "ComponentWithoutParticipants" "ComponentWithoutParticipants.tuc" (Error [ ComponentWithoutParticipants (4, 4, "    StreamComponent parts", "StreamComponent") ])
            case "UndefinedComponentParticipant" "UndefinedComponentParticipant.tuc" (Error [
                UndefinedComponentParticipant (4, 8, "        GenericService parts", "StreamComponent", ["StreamListener"], "GenericService")
                UndefinedComponentParticipant (5, 8, "        Service parts", "StreamComponent", ["StreamListener"], "Service")
            ])
            case "UndefinedComponentParticipant - only one" "UndefinedComponentParticipantInDomain.tuc" (Error [
                UndefinedComponentParticipantInDomain (5, 8, "        [ActivityStream] as \"Activity Stream\"", "Parts", "[ActivityStream]")
            ])
            case "WrongComponentParticipantDomain" "WrongComponentParticipantDomain.tuc" (Error [ WrongComponentParticipantDomain (5, 8, "        Service wrongDomain", "Parts", "Service") ])
            case "InvalidParticipant" "InvalidParticipant.tuc" (Error [ InvalidParticipant (3, 4, "    GenericService domain foo bar") ])
            case "UndefinedParticipantInDomain in participant definition" "UndefinedParticipantInDomain.tuc" (Error [ UndefinedParticipantInDomain (3, 4, "    ServiceNotInDomain parts", "Parts", "ServiceNotInDomain") ])
            case "UndefinedParticipantInDomain in participant definition" "UndefinedParticipant-in-participants.tuc" (Error [ UndefinedParticipantInDomain (3, 4, "    UndefinedParticipantDefinition parts", "Parts", "UndefinedParticipantDefinition") ])
            case "UndefinedParticipant in parts" "UndefinedParticipant-in-parts.tuc" (Error [ UndefinedParticipant (6, 4, "    UndefinedParticipant.Foo", "UndefinedParticipant") ])

            // parts
            case "MissingUseCase" "MissingUseCase.tuc" (Error [ MissingUseCase (7, TucName "without a use-case") ])
            case "SectionWithoutName" "SectionWithoutName.tuc" (Error [ SectionWithoutName (5, 0, "section") ])
            case "IsNotInitiator" "IsNotInitiator.tuc" (Error [ IsNotInitiator (5, 0, "StreamListener", "StreamListener") ])
            case "CalledUndefinedMethod" "CalledUndefinedMethod.tuc" (Error [ CalledUndefinedMethod (7, 4, "    Service.UndefinedMethod", "Service", ["DoSomeWork"], "UndefinedMethod") ])
            case "CalledUndefinedHandler" "CalledUndefinedHandler.tuc" (Error [ CalledUndefinedHandler (7, 4, "    StreamListener.UndefinedHandler", "StreamListener", ["ReadEvent"], "UndefinedHandler") ])
            case "MethodCalledWithoutACaller" "MethodCalledWithoutACaller.tuc" (Error [ MethodCalledWithoutACaller (5, 0, "Service.Method", "Service", "Method") ])
            case "DataPostedWithoutACaller" "DataPostedWithoutACaller.tuc" (Error [ DataPostedWithoutACaller (5, 0, "Person -> [PersonDatabase]", "Person", "[PersonDatabase]") ])
            case "DataReadWithoutACaller" "DataReadWithoutACaller.tuc" (Error [ DataReadWithoutACaller (5, 0, "[PersonDatabase] -> Person", "[PersonDatabase]", "Person") ])
            case "EventPostedWithoutACaller" "EventPostedWithoutACaller.tuc" (Error [ EventPostedWithoutACaller (5, 0, "InputEvent -> [InputStream]", "InputEvent", "[InputStream]") ])
            case "EventReadWithoutACaller" "EventReadWithoutACaller.tuc" (Error [ EventReadWithoutACaller (5, 0, "[InputStream] -> InputEvent", "[InputStream]", "InputEvent") ])
            case "MissingEventHandlerMethodCall" "MissingEventHandlerMethodCall.tuc" (Error [ MissingEventHandlerMethodCall (5, 0, "[InputStream]", "[InputStream]") ])
            case "InvalidMultilineNote" "InvalidMultilineNote.tuc" (Error [ InvalidMultilineNote (6, 4, "    \"\"\"") ])
            case "InvalidMultilineLeftNote" "InvalidMultilineLeftNote.tuc" (Error [ InvalidMultilineLeftNote (5, 0, "\"<\"") ])
            case "InvalidMultilineRightNote" "InvalidMultilineRightNote.tuc" (Error [ InvalidMultilineRightNote (5, 0, "\">\"") ])
            case "DoWithoutACaller" "DoWithoutACaller.tuc" (Error [ DoWithoutACaller (5, 0, "do well.. nothing") ])
            case "DoMustHaveActions" "DoMustHaveActions.tuc" (Error [ DoMustHaveActions (6, 4, "    do") ])
            case "IfWithoutCondition" "IfWithoutCondition.tuc" (Error [ IfWithoutCondition (5, 0, "if") ])
            case "IfMustHaveBody" "IfMustHaveBody.tuc" (Error [ IfMustHaveBody (5, 0, "if true") ])
            case "ElseOutsideOfIf" "ElseOutsideOfIf.tuc" (Error [ ElseOutsideOfIf (5, 0, "else") ])
            case "ElseMustHaveBody" "ElseMustHaveBody.tuc" (Error [ ElseMustHaveBody (8, 4, "    else") ])
            case "GroupWithoutName" "GroupWithoutName.tuc" (Error [ GroupWithoutName (5, 0, "group") ])
            case "GroupMustHaveBody" "GroupMustHaveBody.tuc" (Error [ GroupMustHaveBody (5, 0, "group Without a body") ])
            case "LoopWithoutCondition" "LoopWithoutCondition.tuc" (Error [ LoopWithoutCondition (5, 0, "loop") ])
            case "LoopMustHaveBody" "LoopMustHaveBody.tuc" (Error [ LoopMustHaveBody (5, 0, "loop always") ])
            case "NoteWithoutACaller" "NoteWithoutACaller.tuc" (Error [ NoteWithoutACaller (5, 0, "\"note without a caller\"") ])
            case "UnknownPart" "UnknownPart.tuc" (Error [ UnknownPart (5, 0, "basically whaterver here") ])

            // others
            case "WrongEventName - Post" "WrongEventName-post.tuc" (Error [ WrongEventName (7, 4, "    .InputEvent -> [InputStream]", "it has a wrong format (it must not start/end with . and not contains any spaces)", ".InputEvent") ])
            case "WrongEventName - Read" "WrongEventName-read.tuc" (Error [ WrongEventName (7, 4, "    [InputStream] -> InputEvent.", "it has a wrong format (it must not start/end with . and not contains any spaces)", "InputEvent.") ])
            case "WrongDataName - Post" "WrongDataName-post.tuc" (Error [ WrongDataName (7, 4, "    .Foo -> [PersonDatabase]", "it has a wrong format (it must not start/end with . and not contains any spaces)", ".Foo") ])
            case "WrongDataName - Read" "WrongDataName-read.tuc" (Error [ WrongDataName (7, 4, "    [PersonDatabase] -> InputEvent.", "it has a wrong format (it must not start/end with . and not contains any spaces)", "InputEvent.") ])
            case "WrongEvent - Post" "WrongEvent-post.tuc" (Error [ WrongEvent (7, 4, "    Wrong -> [InputStream]", ["InputEvent"], "Wrong") ])
            case "WrongEvent - Read" "WrongEvent-read.tuc" (Error [ WrongEvent (7, 4, "    [InputStream] -> Wrong", ["InputEvent"], "Wrong") ])
            case "WrongData - Post" "WrongData-post.tuc" (Error [ WrongData (7, 4, "    Wrong -> [PersonDatabase]", ["Person"], "Wrong") ])
            case "WrongData - Read" "WrongData-read.tuc" (Error [ WrongData (7, 4, "    [PersonDatabase] -> Wrong", ["Person"], "Wrong") ])
        ]

[<RequireQualifiedAccess>]
module Event =
    let path = "./Tuc/Fixtures/event"

    let case = case path

    let provider: Case list =
        [
            case "Valid cases" "valid.tuc" (Ok [])
            case "Hr - single case event" "hr.tuc" (Ok [])

            case "InteractionEvent with typo" "wrong-interaction-event.tuc" (Error [
                WrongEvent (
                    10,
                    8,
                    "        InteractionEvents -> [InteractionStream]",
                    ["InteractionEvent"],
                    "InteractionEvents"
                )
            ])

            case "Confirmed with typo" "wrong-confirmed.tuc" (Error [
                WrongEvent (
                    10,
                    24,
                    "        InteractionEvent.Confirmes -> [InteractionStream]",
                    ["Confirmed"; "Rejected"; "Interaction"; "Other"],
                    "InteractionEvent.Confirmes"
                )
            ])

            case "Interaction is too deep" "wrong-interaction-too-deep.tuc" (Error [
                WrongEvent (
                    10,
                    48,
                    "        InteractionEvent.Interaction.Interaction.Interaction -> [InteractionStream]",
                    [],
                    "InteractionEvent.Interaction.Interaction.Interaction"
                )
            ])

            case "Interaction with undefined rejection" "wrong-rejection.tuc" (Error [
                WrongEvent (
                    10,
                    46,
                    "        InteractionEvent.Rejected.UserRejected.Boo -> [InteractionStream]",
                    ["Foo"; "Bar"],
                    "InteractionEvent.Rejected.UserRejected.Boo"
                )
            ])
        ]

[<RequireQualifiedAccess>]
module Example =
    let path = "./Tuc/Fixtures/example"

    let case = case path

    let provider: Case list =
        [
            case "Readme example" "definition.tuc" (Ok [])
        ]

[<RequireQualifiedAccess>]
module MultiTuc =
    let path = "./Tuc/Fixtures/multi-tuc"

    let case = case path

    let provider: Case list =
        [
            case "4 Valid tucs in 1 file" "4-valid.tuc" (Ok [])

            case "3 Different Errors and 1 correct tuc" "3-different-errors.tuc" (Error [
                UndefinedComponentParticipant (4, 8, "        GenericService tests", "StreamComponent", ["StreamListener"], "GenericService")
                UndefinedComponentParticipant (5, 8, "        Service tests", "StreamComponent", ["StreamListener"], "Service")
                ComponentWithoutParticipants (9, 4, "    StreamComponent tests", "StreamComponent")
                CalledUndefinedMethod (27, 4, "    Service.UndefinedMethod", "Service", ["DoSomeWork"], "UndefinedMethod")
            ])
        ]

[<RequireQualifiedAccess>]
module Formatted =
    let path = "./Tuc/Fixtures/formatted"

    let case = case path

    let provider: Case list =
        [
            case "Valid formatted notes, etc." "valid.tuc" (Ok [])
        ]

[<RequireQualifiedAccess>]
module MultiDomain =
    let path = "./Tuc/Fixtures/multi-domain"

    let case = case path

    let provider: Case list =
        [
            case "2 domains with DTOs." "2-domains-with-dtos.tuc" (Ok [])

            //case "Umbiguous services." "service-conflict.tuc" (Error ( ...todo... ))
        ]

[<Tests>]
let parserTests =
    let output = MF.ConsoleStyle.ConsoleStyle()
    let test domainTypes = List.iter (test output domainTypes)

    testList "Tuc.Parser" [
        testCase "should parse parts" <| fun _ ->
            let domainTypes =
                [
                    "partsDomain.fsx"
                ]
                |> Domain.parseDomainsTypes output Parts.path

            Parts.provider |> test domainTypes

        testCase "should parse events" <| fun _ ->
            let domainTypes =
                [
                    "testsDomain.fsx"
                    "hrDomain.fsx"
                ]
                |> Domain.parseDomainsTypes output Event.path

            Event.provider |> test domainTypes

        testCase "should show nice parse errors" <| fun _ ->
            let domainTypes =
                [
                    "partsDomain.fsx"
                ]
                |> Domain.parseDomainsTypes output ParseErrors.path

            ParseErrors.provider |> test domainTypes

        testCase "should parse multiple tucs" <| fun _ ->
            let domainTypes =
                [
                    "testsDomain.fsx"
                ]
                |> Domain.parseDomainsTypes output MultiTuc.path

            MultiTuc.provider |> test domainTypes

        testCase "should parse readme example" <| fun _ ->
            let domainTypes =
                [
                    "consentsDomain.fsx"
                ]
                |> Domain.parseDomainsTypes output Example.path

            Example.provider |> test domainTypes

        testCase "should parse formatted tuc" <| fun _ ->
            let domainTypes =
                [
                    "testsDomain.fsx"
                ]
                |> Domain.parseDomainsTypes output Formatted.path

            Formatted.provider |> test domainTypes

        testCase "should parse multi-domain communication" <| fun _ ->
            let domainTypes =
                [
                    "oneDomain.fsx"
                    "twoDomain.fsx"
                ]
                |> Domain.parseDomainsTypes output MultiDomain.path

            MultiDomain.provider |> test domainTypes
    ]
