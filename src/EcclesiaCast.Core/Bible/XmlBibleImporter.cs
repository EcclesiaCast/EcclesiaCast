using System.Xml.Linq;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// A Bible in an .xml file, whichever of the two shapes it has: Zefania
/// (<c>&lt;XMLBIBLE&gt;&lt;BIBLEBOOK&gt;</c>) or OpenSong / Beblia
/// (<c>&lt;bible&gt;&lt;b&gt;</c> or <c>&lt;bible&gt;&lt;book&gt;</c>).
/// </summary>
public static class XmlBibleImporter
{
    public static ParsedBible Parse(string xml)
    {
        xml = xml.TrimStart('﻿');
        var doc = XDocument.Parse(xml);
        var root = doc.Root?.Name.LocalName ?? "";

        if (doc.Descendants("BIBLEBOOK").Any())
            return ZefaniaBibleImporter.Parse(xml);
        if (string.Equals(root, "bible", StringComparison.OrdinalIgnoreCase))
            return OpenSongBibleImporter.Parse(doc);

        throw new FormatException(
            $"El archivo XML no es una Biblia en un formato conocido (empieza con <{root}>). "
            + "Se leen Zefania (<XMLBIBLE>) y OpenSong (<bible>).");
    }
}
