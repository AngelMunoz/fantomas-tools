module FantomasTools.Client.FantomasOnline.View

open System.Text.RegularExpressions
open Fable.React
open Fable.React.Props
open FantomasOnline.Shared
open FantomasTools.Client
open FantomasTools.Client.Editor
open FantomasTools.Client.FantomasOnline.Model

let private mapToOption dispatch (model: Model) (key, fantomasOption) =
    let editor =
        let label =
            a [
                Href $"https://fsprojects.github.io/fantomas/docs/end-users/Configuration.html#{toEditorConfigName key}"
                Target "_blank"
            ] [ str key ]

        match fantomasOption with
        | FantomasOption.BoolOption(o, _, v) ->
            SettingControls.toggleButton
                (fun _ -> UpdateOption(key, BoolOption(o, key, true)) |> dispatch)
                (fun _ -> UpdateOption(key, BoolOption(o, key, false)) |> dispatch)
                "true"
                "false"
                label
                v

        | FantomasOption.IntOption(o, _, v) ->
            let onChange (nv: string) =
                if Regex.IsMatch(nv, "\\d+") then
                    let v = nv |> int

                    UpdateOption(key, IntOption(o, key, v)) |> dispatch

            SettingControls.input key onChange label "integer" v
        | FantomasOption.MultilineFormatterTypeOption(o, _, v) ->
            SettingControls.toggleButton
                (fun _ ->
                    UpdateOption(key, MultilineFormatterTypeOption(o, key, "character_width"))
                    |> dispatch)
                (fun _ ->
                    UpdateOption(key, MultilineFormatterTypeOption(o, key, "number_of_items"))
                    |> dispatch)
                "CharacterWidth"
                "NumberOfItems"
                label
                (v = "character_width")
        | FantomasOption.EndOfLineStyleOption(o, _, v) ->
            SettingControls.toggleButton
                (fun _ -> UpdateOption(key, EndOfLineStyleOption(o, key, "crlf")) |> dispatch)
                (fun _ -> UpdateOption(key, EndOfLineStyleOption(o, key, "lf")) |> dispatch)
                "CRLF"
                "LF"
                label
                (v = "crlf")
        | FantomasOption.MultilineBracketStyleOption(o, _, v) ->
            let mkButton (value: string) =
                let label =
                    let capital = System.Char.ToUpper value.[0]
                    $"{capital}{value.[1..]}".Replace("_", " ")

                let activeBtnClass =
                    if v <> value then
                        Style.BtnOutlineSecondary
                    else
                        $"{Style.BtnSecondary} {Style.TextWhite}"

                button [
                    ClassName $"{Style.Btn} {activeBtnClass}"
                    Key value
                    OnClick(fun _ -> UpdateOption(key, MultilineBracketStyleOption(o, key, value)) |> dispatch)
                ] [ str label ]

            div [ ClassName Style.Mb3 ] [
                Standard.label [ ClassName Style.FormLabel ] [ label ]
                br []
                div [ ClassName $"{Style.BtnGroup}" ] [
                    yield mkButton "cramped"
                    yield mkButton "aligned"
                    if model.Mode = FantomasMode.V5 then
                        yield mkButton "experimental_stroustrup"
                    if model.Mode = FantomasMode.Main || model.Mode = FantomasMode.Preview then
                        yield mkButton "stroustrup"
                ]
            ]

    div [ Key key ] [ editor ]

let options model dispatch =
    let optionList =
        Map.toList model.UserOptions
        |> List.sortBy fst
        |> fun optionList ->
            if System.String.IsNullOrWhiteSpace model.SettingsFilter then
                optionList
            else
                let settingsFilter =
                    model.SettingsFilter
                        .Replace("fsharp_", "")
                        .Replace("_", "")
                        .Replace(" ", "")
                        .ToLowerInvariant()

                optionList
                |> List.filter (fun (n, _) ->
                    let setting = n.ToLowerInvariant()
                    setting.Contains(settingsFilter))

    optionList |> List.map (mapToOption dispatch model) |> ofList

type GithubIssue =
    { BeforeHeader: string
      BeforeContent: string
      AfterHeader: string
      AfterContent: string
      Description: string
      Title: string
      DefaultOptions: FantomasOption list
      UserOptions: Map<string, FantomasOption>
      Version: string
      IsFsi: bool }

let githubIssueUri (githubIssue: GithubIssue) =
    let location = Browser.Dom.window.location

    let config =
        githubIssue.UserOptions
        |> Map.toList
        |> List.map snd
        |> List.sortBy sortByOption

    let defaultValues = githubIssue.DefaultOptions |> List.sortBy sortByOption

    let options =
        let changedOptions =
            Seq.zip config defaultValues
            |> Seq.toArray
            |> Seq.choose (fun (userV, defV) -> if userV <> defV then Some userV else None)
            |> Seq.toList

        if List.isEmpty changedOptions then
            "Default Fantomas configuration"
        else
            changedOptions
            |> Seq.map (fun opt -> sprintf "                %s = %s" (getOptionKey opt) (optionValue opt))
            |> String.concat "\n"
            |> sprintf
                """```fsharp
    { config with
%s }
```"""

    let codeTemplate header code =
        sprintf
            """
#### %s

```fsharp
%s
```
            """
            header
            code

    let left, right =
        codeTemplate githubIssue.BeforeHeader githubIssue.BeforeContent,
        codeTemplate githubIssue.AfterHeader githubIssue.AfterContent

    let fileType =
        if githubIssue.IsFsi then
            "\n*Signature file*"
        else
            System.String.Empty

    let body =
        (sprintf
            """
<!--

    Please only use this to create issues.
    If you wish to suggest a feature,
    please fill in the feature request template at https://github.com/fsprojects/fantomas/issues/new/choose

-->
Issue created from [fantomas-online](%s)

%s
%s
#### Problem description

%s

#### Extra information

- [ ] The formatted result breaks my code.
- [ ] The formatted result gives compiler warnings.
- [ ] I or my company would be willing to help fix this.

#### Options

Fantomas %s

%s
%s

<sub>Did you know that you can ignore files when formatting from fantomas-tool or the FAKE targets by using a [.fantomasignore file](https://fsprojects.github.io/fantomas/docs/end-users/IgnoreFiles.html)?</sub>
        """
            location.href
            left
            right
            githubIssue.Description
            githubIssue.Version
            options
            fileType)
        |> System.Uri.EscapeDataString

    let uri =
        sprintf "https://github.com/fsprojects/fantomas/issues/new?title=%s&body=%s" githubIssue.Title body

    uri |> Href

let private createGitHubIssue code isFsi model =
    let description =
        """Please describe here the Fantomas problem you encountered.
                    Check out our [Contribution Guidelines](https://github.com/fsprojects/fantomas/blob/main/CONTRIBUTING.md#bug-reports)."""

    let bh, bc, ah, ac =
        match model.State with
        | FormatError e -> "Code", code, "Error", e
        | FormatResult result -> "Code", code, "Result", (Option.defaultValue result.FirstFormat result.SecondFormat)
        | _ -> "Code", code, "", ""

    if System.String.IsNullOrWhiteSpace(code) then
        span [ ClassName $"{Style.TextMuted} {Style.Me2}" ] [ str "Looks wrong? Try using the main version!" ]
    else
        match model.Mode with
        | Main
        | Preview ->
            let githubIssue =
                { BeforeHeader = bh
                  BeforeContent = bc
                  AfterHeader = ah
                  AfterContent = ac
                  Description = description
                  Title = "<Insert meaningful title>"
                  DefaultOptions = model.DefaultOptions
                  UserOptions = model.UserOptions
                  Version = model.Version
                  IsFsi = isFsi }

            a [
                ClassName $"{Style.Btn} {Style.BtnOutlineDanger}"
                githubIssueUri githubIssue
                Target "_blank"
            ] [ str "Looks wrong? Create an issue!" ]
        | _ -> span [ ClassName $"{Style.TextMuted} {Style.Me2}" ] [ str "Looks wrong? Try using the main version!" ]

let private viewErrors (model: Model) isFsi result isIdempotent errors =
    let errors =
        match errors with
        | [] -> []
        | errors ->
            let badgeColor (e: ASTError) =
                match e.Severity with
                | ASTErrorSeverity.Error -> Style.TextBgDanger
                | ASTErrorSeverity.Warning -> Style.TextBgWarning
                | _ -> Style.TextBgInfo

            errors
            |> List.mapi (fun i e ->
                li [ Key(sprintf "ast-error-%i" i) ] [
                    strong [] [
                        str (
                            sprintf
                                "(%i,%i) (%i, %i)"
                                e.Range.StartLine
                                e.Range.StartCol
                                e.Range.EndLine
                                e.Range.EndCol
                        )
                    ]
                    span [ ClassName $"{Style.Badge} {badgeColor}" ] [ str (e.Severity.ToString()) ]
                    span [ ClassName $"{Style.Badge} {Style.TextBgDark}"; Title "ErrorNumber" ] [ ofInt e.ErrorNumber ]
                    span [ ClassName $"{Style.Badge} {Style.TextBgLight}"; Title "SubCategory" ] [ str e.SubCategory ]
                    p [] [ str e.Message ]
                ])

    let idempotency =
        if isIdempotent then
            None
        else
            let githubIssue =
                { BeforeHeader = "Formatted code"
                  BeforeContent = result.FirstFormat
                  AfterHeader = "Reformatted code"
                  AfterContent = Option.defaultValue result.FirstFormat result.SecondFormat
                  Description = "Fantomas was not able to produce the same code after reformatting the result."
                  Title = "Idempotency problem when <add use-case>"
                  DefaultOptions = model.DefaultOptions
                  UserOptions = model.UserOptions
                  Version = model.Version
                  IsFsi = isFsi }

            div [ ClassName Style.IdempotentError ] [
                h6 [] [ str "The result was not idempotent" ]
                str "Fantomas was able to format the code, but when formatting the result again, the code changed."
                br []
                str "The result after the first format is being displayed."
                br []
                a [
                    ClassName $"{Style.Btn} {Style.BtnDanger}"
                    githubIssueUri githubIssue
                    Target "_blank"
                ] [ str "Report idempotancy issue" ]
            ]
            |> Some

    if not isIdempotent || not (List.isEmpty errors) then
        ul [ Id "ast-errors"; ClassName "" ] [ ofOption idempotency; ofList errors ]
        |> Some
    else
        None

let view isFsi model =
    match model.State with
    | EditorState.LoadingFormatRequest
    | EditorState.LoadingOptions -> Loader.loader
    | EditorState.OptionsLoaded -> null
    | EditorState.FormatResult result ->
        let formattedCode, isIdempotent, astErrors =
            match result.SecondFormat with
            | Some sf when sf = result.FirstFormat -> sf, true, result.SecondValidation
            | Some _ -> result.FirstFormat, false, result.FirstValidation
            | None -> result.FirstFormat, true, result.FirstValidation

        div [ ClassName $"{Style.TabResult} {Style.FantomasResult}" ] [
            div [ ClassName Style.FantomasEditorContainer ] [
                ReadOnlyEditor [
                    MonacoEditorProp.DefaultValue formattedCode
                    MonacoEditorProp.Options(MonacoEditorProp.rulerOption model.MaxLineLength)
                ]
            ]
            ofOption (viewErrors model isFsi result isIdempotent astErrors)
        ]

    | EditorState.FormatError error ->
        div [ ClassName Style.TabResult ] [ ReadOnlyEditor [ MonacoEditorProp.DefaultValue error ] ]

let private userChangedSettings (model: Model) =
    model.SettingsChangedByTheUser |> List.isEmpty |> not

let commands code isFsi model dispatch =
    let formatButton =
        button [
            ClassName $"{Style.Btn} {Style.BtnPrimary} {Style.TextWhite}"
            OnClick(fun _ -> dispatch Msg.Format)
        ] [ str "Format" ]

    let copySettingButton =
        if userChangedSettings model then
            button [
                ClassName $"{Style.Btn} {Style.BtnSecondary} {Style.TextWhite}"
                OnClick(fun _ -> dispatch CopySettings)
            ] [ str "Copy settings" ]
            |> Some
        else
            None

    match model.State with
    | EditorState.LoadingOptions -> []
    | EditorState.LoadingFormatRequest -> [ formatButton; ofOption copySettingButton ]
    | EditorState.OptionsLoaded
    | EditorState.FormatResult _
    | EditorState.FormatError _ -> [ createGitHubIssue code isFsi model; formatButton; ofOption copySettingButton ]
    |> fragment []

let settings isFsi model dispatch =
    match model.State with
    | EditorState.LoadingOptions -> span [ ClassName $"{Style.SpinnerBorder} {Style.TextPrimary}" ] []
    | _ ->
        let fantomasMode =
            [ FantomasMode.V4, "4.x"
              FantomasMode.V5, "5.x"
              FantomasMode.Main, "Main"
              FantomasMode.Preview, "v6 preview" ]
            |> List.map (fun (m, l) ->
                { IsActive = model.Mode = m
                  Label = l
                  OnClick = (fun _ -> ChangeMode m |> dispatch) }
                : SettingControls.MultiButtonSettings)
            |> SettingControls.multiButton "Mode"

        let fileExtension =
            SettingControls.toggleButton
                (fun _ -> SetFsiFile true |> dispatch)
                (fun _ -> SetFsiFile false |> dispatch)
                "*.fsi"
                "*.fs"
                (str "File extension")
                isFsi

        let options = options model dispatch

        let searchBox =
            div [ ClassName $"{Style.My3} {Style.BorderBottom}" ] [
                div [] [
                    label [ ClassName Style.DBlock ] [
                        strong [ ClassName $"{Style.H4} {Style.TextCenter} {Style.DBlock} {Style.Mb2}" ] [
                            str "Filter settings"
                        ]
                    ]
                    input [
                        Type "search"
                        ClassName Style.FormControl
                        DefaultValue model.SettingsFilter
                        Placeholder "Filter settings"
                        Props.OnChange(fun (ev: Browser.Types.Event) -> ev.Value |> UpdateSettingsFilter |> dispatch)
                    ]
                ]
            ]

        fragment [] [
            VersionBar.versionBar (sprintf "Version: %s" model.Version)
            fantomasMode
            fileExtension
            hr []
            searchBox
            options
        ]
