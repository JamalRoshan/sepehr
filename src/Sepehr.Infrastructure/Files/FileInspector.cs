using System;
using System.Collections.Generic;
using System.IO;

namespace Sepehr.Infrastructure.Files
{
    public enum FileVerdict { Accepted, InvalidName, Executable, ContentMismatch }

    /// <summary>
    /// کنترل نام، پسوند و Magic Bytes. جایگزین اسکن بدافزار نیست و به‌تنهایی فایل را امن اعلام نمی‌کند.
    /// MIME ارسالی کلاینت قابل اعتماد نیست.
    /// </summary>
    public static class FileInspector
    {
        private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".com", ".scr", ".pif", ".cpl", ".msi", ".msp", ".msix", ".appx",
            ".bat", ".cmd", ".ps1", ".psm1", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh",
            ".hta", ".lnk", ".reg", ".jar", ".apk", ".sh", ".sys", ".msc", ".scf", ".url",
            ".application", ".gadget", ".inf", ".chm", ".iso", ".img", ".vhd", ".vhdx",
            ".docm", ".xlsm", ".pptm", ".dotm", ".xltm", ".xlam", ".ppam"
        };

        private static readonly byte[] Zip = { 0x50, 0x4B, 0x03, 0x04 };

        private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".gif"] = new[] { "GIF87a"u8.ToArray(), "GIF89a"u8.ToArray() },
            [".pdf"] = new[] { "%PDF-"u8.ToArray() },
            [".zip"] = new[] { Zip, new byte[] { 0x50, 0x4B, 0x05, 0x06 } },
            [".docx"] = new[] { Zip },
            [".xlsx"] = new[] { Zip },
            [".pptx"] = new[] { Zip },
            [".rar"] = new[] { new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 } }
        };

        /// <param name="header">حداقل 16 بایت اول فایل (یا کل فایل اگر کوچک‌تر است).</param>
        public static FileVerdict Inspect(string fileName, ReadOnlySpan<byte> header)
        {
            if (!IsSafeName(fileName)) return FileVerdict.InvalidName;

            // ویندوز نقطه و فاصله انتهایی را حذف می‌کند: "a.exe. " همان a.exe است.
            var extension = Path.GetExtension(fileName.TrimEnd('.', ' '));

            if (ExecutableExtensions.Contains(extension) || HasExecutableHeader(header))
                return FileVerdict.Executable;

            if (Signatures.TryGetValue(extension, out var expected))
            {
                foreach (var signature in expected)
                {
                    if (header.StartsWith(signature)) return FileVerdict.Accepted;
                }

                return FileVerdict.ContentMismatch;
            }

            return FileVerdict.Accepted;
        }

        private static bool IsSafeName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255) return false;
            if (fileName.TrimEnd('.', ' ').Length == 0) return false;

            foreach (var c in fileName)
            {
                // جداکننده مسیر، ADS و نویسه‌های کنترلی/جهت‌دهی (جعل پسوند با RLO)
                if (c is '/' or '\\' or ':' || char.IsControl(c)) return false;
                if (c is >= '‪' and <= '‮' or >= '⁦' and <= '⁩') return false;
            }

            return true;
        }

        private static bool HasExecutableHeader(ReadOnlySpan<byte> header) =>
            header.StartsWith("MZ"u8)
            || header.StartsWith("#!"u8)
            || header.StartsWith(new byte[] { 0x7F, 0x45, 0x4C, 0x46 });
    }
}
