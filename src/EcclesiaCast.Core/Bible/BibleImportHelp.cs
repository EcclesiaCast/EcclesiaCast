using System.Security;
using System.Text;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// What a person needs to bring their own Bible in: a small, real example of
/// each file the importer reads, and a text to hand to an AI chat so it turns
/// (or checks) a file into one of those shapes. The examples are made with the
/// program's own writer or checked by its own readers (see the tests), so they
/// cannot drift away from what the importer accepts.
/// </summary>
public static class BibleImportHelp
{
    /// <summary>
    /// The text of the example: Genesis 1:1-3 and John 3:16-17 of the Reina-Valera
    /// 1909, which is in the public domain.
    /// </summary>
    public static ParsedBible SampleBible() => new(
    [
        new ParsedBook(1, "Génesis",
        [
            new ParsedVerse(1, 1, "En el principio crió Dios los cielos y la tierra."),
            new ParsedVerse(1, 2, "Y la tierra estaba desordenada y vacía, y las tinieblas estaban sobre la haz del abismo, y el Espíritu de Dios se movía sobre la haz de las aguas."),
            new ParsedVerse(1, 3, "Y dijo Dios: Sea la luz: y fué la luz."),
        ]),
        new ParsedBook(43, "Juan",
        [
            new ParsedVerse(3, 16, "Porque de tal manera amó Dios al mundo, que ha dado á su Hijo unigénito, para que todo aquel que en él creyere, no se pierda, mas tenga vida eterna."),
            new ParsedVerse(3, 17, "Porque no envió Dios á su Hijo al mundo para que condene al mundo, mas para que el mundo sea salvo por él."),
        ]),
    ]);

    /// <summary>The example as the «array of books» JSON file: the shape the program itself exports.</summary>
    public static string ExampleJson() => JsonBibleExporter.ToJson(SampleBible());

    /// <summary>The example as a Zefania XML file.</summary>
    public static string ExampleZefaniaXml()
    {
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine("<XMLBIBLE biblename=\"Ejemplo\">");
        foreach (var book in SampleBible().Books)
        {
            xml.AppendLine($"  <BIBLEBOOK bnumber=\"{book.Number}\" bname=\"{SecurityElement.Escape(book.Name)}\">");
            foreach (var chapter in book.Verses.GroupBy(v => v.Chapter))
            {
                xml.AppendLine($"    <CHAPTER cnumber=\"{chapter.Key}\">");
                foreach (var verse in chapter)
                    xml.AppendLine($"      <VERS vnumber=\"{verse.Verse}\">{SecurityElement.Escape(verse.Text)}</VERS>");
                xml.AppendLine("    </CHAPTER>");
            }

            xml.AppendLine("  </BIBLEBOOK>");
        }

        xml.AppendLine("</XMLBIBLE>");
        return xml.ToString();
    }

    /// <summary>The first lines of the JSON example, enough to see its shape without scrolling through 66 books.</summary>
    public static string JsonPreview(int lines = 14)
    {
        var all = ExampleJson().Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        return string.Join('\n', all.Take(lines)) + "\n  ... (así hasta el libro 66, Apocalipsis)";
    }

    /// <summary>
    /// A text to paste in an AI chat, together with the Bible to convert. It
    /// says what the program reads, and — because a whole Bible does not fit in
    /// one answer — asks for a small conversion program instead of the converted text.
    /// </summary>
    public static string InstructionsForAi() => """
        Necesito convertir una Biblia a un formato que lee un programa de proyección para iglesias (EcclesiaCast). Hablame en castellano, simple, como si yo no supiera programar.

        EL FORMATO QUE NECESITO (JSON):
        - Es un arreglo con los 66 libros, en el orden de siempre: de Génesis (el 1.º) a Apocalipsis (el 66.º). La posición en el arreglo ES el número del libro, así que tienen que estar los 66, aunque alguno quede vacío.
        - Cada libro es un objeto con "name" (el nombre del libro, en castellano) y "chapters".
        - "chapters" es un arreglo de capítulos, en orden (el 1.º es el capítulo 1).
        - Cada capítulo es un arreglo de textos, uno por versículo, en orden (el 1.º es el versículo 1).
        - Cada versículo es sólo el texto, sin el número delante y sin llaves ni etiquetas. No resumas ni cambies ninguna palabra.
        - Si falta un versículo en el medio, dejá "" (texto vacío) en su lugar para no correr los números.
        - Codificación UTF-8, con los acentos bien.

        EJEMPLO (sólo el principio; en el archivo real van los 66 libros):
        [
          { "name": "Génesis", "chapters": [
              [ "En el principio crió Dios los cielos y la tierra.", "Y la tierra estaba desordenada y vacía..." ],
              [ "Fueron pues acabados los cielos y la tierra, y todo su ornamento." ]
          ] },
          { "name": "Éxodo", "chapters": [ ... ] }
        ]

        TAMBIÉN SIRVE el formato Zefania XML: <XMLBIBLE><BIBLEBOOK bnumber="43" bname="Juan"><CHAPTER cnumber="3"><VERS vnumber="16">texto</VERS></CHAPTER></BIBLEBOOK></XMLBIBLE>, donde bnumber es el número del libro (de 1 a 66).

        COMO UNA BIBLIA ENTERA NO ENTRA EN UNA RESPUESTA, no me escribas el archivo convertido. En su lugar:
        1. Preguntame cómo es el archivo o el texto que tengo (si es un .txt, un .docx, un .pdf, una página web, otro formato) y pedime que te pegue unas líneas de ejemplo.
        2. Escribime un programa chico en Python que lea mi archivo y escriba el JSON de arriba, y explicame paso a paso cómo instalarlo y correrlo en Windows.
        3. Después decime cómo revisar el resultado: cuántos libros salen, que Juan 3:16 sea el versículo correcto, y que no haya capítulos corridos.

        Tené en cuenta que sólo puedo usar una Biblia que tenga derecho a usar (una versión libre, o una que ya tenga licencia para mi iglesia).
        """;
}
