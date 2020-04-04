module FantomasTools.Client.Navigation

open Elmish
open Elmish.UrlParser
open Elmish.Navigation

open FantomasTools.Client
open FantomasTools.Client.Model
open FantomasTools.Client.FantomasOnline.Model

let private route : Parser<ActiveTab->_,_> =
    UrlParser.oneOf [
        map ActiveTab.HomeTab (s "")
        map ActiveTab.TriviaTab (s "trivia")
        map ActiveTab.TokensTab (s "tokens")
        map ActiveTab.ASTTab (s "ast")
        map (ActiveTab.FantomasTab (Previous)) (s "fantomas" </> s "previous")
        map (ActiveTab.FantomasTab (Latest)) (s "fantomas" </> s "latest")
        map (ActiveTab.FantomasTab (Preview)) (s "fantomas" </> s "preview")
    ]
let parser : (Browser.Types.Location -> ActiveTab option) = parseHash route

let urlUpdate (result:Option<ActiveTab>) model =
    match result with
    | Some tab ->
        let cmd =
            if not (System.String.IsNullOrWhiteSpace model.SourceCode) then
                match tab with
                | TriviaTab -> Cmd.ofMsg (Trivia.Model.GetTrivia) |> Cmd.map Msg.TriviaMsg
                | TokensTab -> Cmd.ofMsg (FSharpTokens.Model.GetTokens) |> Cmd.map Msg.FSharpTokensMsg
                | ASTTab -> Cmd.ofMsg (ASTViewer.Model.DoParse) |> Cmd.map Msg.ASTMsg
                | _ -> Cmd.none
            else
                Cmd.none

        { model with ActiveTab = tab }, cmd
    | None ->
        ( { model with ActiveTab = HomeTab }, Navigation.modifyUrl "#" ) // no matching route - go home

let toHash =
    function
    | HomeTab -> "#/"
    | TriviaTab -> "#/trivia"
    | TokensTab -> "#/tokens"
    | ASTTab -> "#/ast"
    | FantomasTab (Previous) -> "#/fantomas/previous"
    | FantomasTab (Latest) -> "#/fantomas/latest"
    | FantomasTab (Preview) -> "#/fantomas/preview"