﻿[<RequireQualifiedAccess>]
module FSharpFar.Tips
open System
open System.IO
open System.Xml
open FSharp.Compiler.SourceCodeServices

let private trimTip =
    let chars = [| '\r'; '\n' |]
    fun (str: string) -> str.TrimEnd().TrimStart chars

let rec private getContent (elem: XmlElement) : string =
    use w = new StringWriter ()
    for node in elem.ChildNodes do
        match node.NodeType with
        | XmlNodeType.Text -> w.Write node.Value
        | XmlNodeType.Element ->
            let e = node :?> XmlElement
            match e.Name with
            | "see" ->
                fprintf w "`%s`" (e.GetAttribute "cref")
            | "para" ->
                w.WriteLine ()
                w.Write (getContent e)
            | "c" ->
                fprintf w "`%s`" e.InnerText
            | "code" ->
                w.WriteLine (trimTip e.InnerText)
            | "paramref" ->
                fprintf w "`%s`" (e.GetAttribute "name")
            | _ ->
                w.Write node.OuterXml
        | _ ->
            w.Write node.OuterXml
    trimTip (w.ToString ())

/// XmlElement representing a doc member.
type private XmlDocMember (elem: XmlElement) =

    let getFirstChild name =
        let nodes = elem.GetElementsByTagName name
        if nodes.Count = 0 then
            ""
        else
            getContent (nodes.[0] :?> XmlElement)

    let getChildren name =
        elem.GetElementsByTagName name
        |> Seq.cast<XmlElement>
        |> Seq.toList

    member __.Format (full) =
        use w = new StringWriter ()
        let formatMessage = formatMessage (messageWidth full)
        let write text = text |> strZipSpace |> formatMessage |> w.WriteLine

        let summary = getFirstChild "summary"
        if full then
            w.WriteLine ()
        summary
        |> write

        let parameters = getChildren "param"
        if not parameters.IsEmpty then
            if full then
                w.WriteLine ()
                w.WriteLine "PARAMETERS:"
            for e in parameters do
                sprintf "- %s: %s" (e.GetAttribute "name") (getContent e)
                |> write
        
        if full then
            let typeparam = getChildren "typeparam"
            if not typeparam.IsEmpty then
                w.WriteLine ()
                w.WriteLine "TYPE PARAMETERS:"
                for e in typeparam do
                    sprintf "- %s: %s" (e.GetAttribute "name") (getContent e)
                    |> write

            let returns = getFirstChild "returns"
            if returns.Length > 0 then
                w.WriteLine ()
                w.WriteLine "RETURNS:"
                returns
                |> write

            let exceptions = getChildren "exception"
            if not exceptions.IsEmpty then
                w.WriteLine ()
                w.WriteLine "EXCEPTIONS:"
                for e in exceptions do
                    sprintf "- `%s`: %s" (e.GetAttribute "cref") (getContent e)
                    |> write
        
            let remarks = getFirstChild "remarks"
            if remarks.Length > 0 then
                w.WriteLine ()
                w.WriteLine "REMARKS:"
                remarks
                |> write

        w.ToString ()

/// Reads and gets the member map, as much as it can read before any problem.
let private readXmlDocMap (reader: XmlReader) =
    let mutable map = Map.empty
    let doc = XmlDocument ()
    try
        while reader.Read () do
            if reader.NodeType = XmlNodeType.Element && reader.Name = "member" then
                let elem = doc.ReadNode reader :?> XmlElement
                let name = elem.GetAttribute "name"
                map <- Map.add name (XmlDocMember elem) map
    with _ -> ()
    map

/// Gets some found XML file.
let private tryXmlFile dllFile =
    let xmlFile = Path.ChangeExtension (dllFile, "xml")
    if File.Exists xmlFile then
        Some xmlFile
    else
    let dir = Path.GetDirectoryName xmlFile
    if Path.GetFileName dir = "v4.6.2" then
        let xmlFile = sprintf @"%s\v4.X\%s" (Path.GetDirectoryName dir) (Path.GetFileName xmlFile)
        if File.Exists xmlFile then
            Some xmlFile
        else
        None
    else
    None

/// Gets some cached or reads a new member map from the file.
let private tryXmlDocMap =
    let cache = System.Collections.Concurrent.ConcurrentDictionary<string, Map<string, XmlDocMember>> StringComparer.OrdinalIgnoreCase
    fun dllFile ->
        match tryXmlFile dllFile with
        | Some xmlFile ->
            let add _ =
                use reader = XmlReader.Create xmlFile
                readXmlDocMap reader
            Some (cache.GetOrAdd (xmlFile, add))
        | None ->
            None

let private formatComment comment full =
    match comment with
    | FSharpXmlDoc.XmlDocFileSignature (dllFile, memberName) ->
        match tryXmlDocMap dllFile with
        | Some map ->
            match map.TryFind memberName with
            | Some doc ->
                doc.Format full
            | None ->
                ""
        | None ->
            ""
    | FSharpXmlDoc.Text text ->
        trimTip text
    | FSharpXmlDoc.None ->
        ""

let private formatTip (FSharpToolTipText tips) full =
    tips
    |> List.choose (function
        | FSharpToolTipElement.Group items ->
            let commentsBySignatures = [
                for it in items do
                    yield it.MainDescription, formatComment it.XmlDoc full
            ]
            Some commentsBySignatures
        | FSharpToolTipElement.CompositionError error ->
            Some ["<error>", error]
        | FSharpToolTipElement.None ->
            None
    )

let format tips full =
    use w = new StringWriter ()
    let messageWidth = messageWidth full
    let formatMessage = formatMessage messageWidth

    for commentsBySignatures in formatTip tips full do
        for signature, comment in commentsBySignatures do
            if full && w.GetStringBuilder().Length > 0 then
                w.WriteLine ()
                w.WriteLine ("".PadRight (messageWidth, '~'))
            w.WriteLine (formatMessage signature)
            if comment.Length > 0 then
                w.WriteLine comment
    w.ToString ()
