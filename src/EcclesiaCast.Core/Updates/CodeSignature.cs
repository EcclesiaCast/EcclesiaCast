using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EcclesiaCast.Core.Updates;

/// <summary>
/// What a program file says about who signed it.
/// </summary>
/// <param name="Signer">
/// The subject of the certificate that signed the file, or null when it has no
/// signature (or it can not be read). The subject and not the thumbprint, so
/// the yearly renewal of the certificate does not make a good update look like
/// somebody else's.
/// </param>
/// <param name="IsTrusted">Windows checked the signature and found it intact and issued by a trusted authority.</param>
public sealed record SignatureInfo(string? Signer, bool IsTrusted)
{
    public static readonly SignatureInfo Unsigned = new(null, false);

    public bool IsSigned => Signer is not null;
}

/// <summary>Reads the digital signature of a file. An interface so a test can say what a file is signed by.</summary>
public interface ISignatureReader
{
    SignatureInfo Read(string path);
}

/// <summary>
/// The rule for accepting an installer. The installed program is the
/// reference: if it carries a signature, the one that replaces it has to carry
/// the same signer's, intact. If the installed program is not signed (as it
/// has not been so far), there is nothing to compare with and an installer
/// without a signature is accepted too; the SHA-256 that GitHub publishes is
/// the check that remains.
/// </summary>
public static class SignaturePolicy
{
    public static bool Accepts(SignatureInfo installed, SignatureInfo candidate)
    {
        if (!installed.IsSigned)
            return true;

        return candidate.IsSigned
            && candidate.IsTrusted
            && string.Equals(candidate.Signer, installed.Signer, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The signature of the installed program: the one of the first file that
    /// has one. EcclesiaCast.App.exe is looked at first and the uninstaller
    /// second, because they are what the installer leaves in the folder.
    /// </summary>
    public static SignatureInfo OfInstalled(ISignatureReader reader, params string?[] files)
    {
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                continue;

            var info = reader.Read(file);
            if (info.IsSigned)
                return info;
        }

        return SignatureInfo.Unsigned;
    }
}

/// <summary>
/// Reads Authenticode signatures with Windows' own functions: the signer from
/// the file, and whether Windows trusts it from WinVerifyTrust.
/// </summary>
public sealed class WindowsSignatureReader : ISignatureReader
{
    public SignatureInfo Read(string path)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(path))
            return SignatureInfo.Unsigned;

        string subject;
        try
        {
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
            subject = certificate.Subject;
        }
        catch (CryptographicException)
        {
            // No Authenticode signature in the file.
            return SignatureInfo.Unsigned;
        }

        return new SignatureInfo(subject, IsTrustedByWindows(path));
    }

    private static bool IsTrustedByWindows(string path)
    {
        var action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE"); // WINTRUST_ACTION_GENERIC_VERIFY_V2
        var file = new WintrustFileInfo
        {
            cbStruct = (uint)Marshal.SizeOf<WintrustFileInfo>(),
            pcwszFilePath = Marshal.StringToCoTaskMemUni(path),
        };
        var filePointer = Marshal.AllocCoTaskMem(Marshal.SizeOf<WintrustFileInfo>());
        Marshal.StructureToPtr(file, filePointer, fDeleteOld: false);

        var data = new WintrustData
        {
            cbStruct = (uint)Marshal.SizeOf<WintrustData>(),
            dwUIChoice = 2,                       // WTD_UI_NONE
            fdwRevocationChecks = 0,              // WTD_REVOKE_NONE
            dwUnionChoice = 1,                    // WTD_CHOICE_FILE
            pFile = filePointer,
            dwStateAction = 1,                    // WTD_STATEACTION_VERIFY
            // Do not go to the network for the revocation list: an installer
            // must not be refused only because the church's internet is slow.
            dwProvFlags = 0x10 | 0x1000,          // WTD_REVOCATION_CHECK_NONE | WTD_CACHE_ONLY_URL_RETRIEVAL
        };

        try
        {
            var result = WinVerifyTrust(new IntPtr(-1), ref action, ref data);
            return result == 0;
        }
        finally
        {
            data.dwStateAction = 2;               // WTD_STATEACTION_CLOSE
            WinVerifyTrust(new IntPtr(-1), ref action, ref data);
            Marshal.FreeCoTaskMem(file.pcwszFilePath);
            Marshal.FreeCoTaskMem(filePointer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WintrustFileInfo
    {
        public uint cbStruct;
        public IntPtr pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WintrustData
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }

    [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false)]
    private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid actionId, ref WintrustData data);
}
