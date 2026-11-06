using System;
using System.IO;
using UnityEngine;

namespace SpiritTide
{
    public static class TideStorage
    {
        public static string Diagnostic {get;private set;} = "";
        [Serializable] private class Envelope { public int format = 1; public string payload; public string checksum; }
        private static string Hash(string text)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }
        public static TideProfile Load(string path, out string notice)
        {
            notice = "";
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return new TideProfile();
            foreach (string candidate in new[] {path, path + ".bak"})
            {
                TideProfile profile;
                if (TryRead(candidate, out profile))
                {
                    if (candidate.EndsWith(".bak", StringComparison.Ordinal)) notice = "저장 복구본을 불러왔습니다.";
                    return profile;
                }
            }
            notice = "저장을 읽지 못해 새 임시 진행으로 시작했습니다. 기존 파일은 보존했습니다.";
            return new TideProfile();
        }
        private static bool TryRead(string path, out TideProfile profile)
        {
            profile = null;
            try
            {
                if (!File.Exists(path)) return false;
                Envelope e = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
                if (e == null || e.format != 1 || string.IsNullOrEmpty(e.payload) || Hash(e.payload) != e.checksum) return false;
                TideProfile p = JsonUtility.FromJson<TideProfile>(e.payload);
                if (p == null || p.version != 1) return false;
                TideCatalog.Sanitize(p); profile = p; return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { return false; }
        }
        private static void PreserveCorrupt(string path)
        {
            TideProfile ignored;
            if (File.Exists(path) && !TryRead(path, out ignored))
                File.Copy(path, CorruptPath(path), false);
        }
        private static string CorruptPath(string path){return path+".corrupt-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+"-"+Guid.NewGuid().ToString("N");}
        public static bool Save(string path, TideProfile profile, out string error)
        {
            error = ""; Diagnostic = "";
            try
            {
                TideCatalog.Sanitize(profile);
                string dir = Path.GetDirectoryName(path); if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                string payload = JsonUtility.ToJson(profile);
                string text = JsonUtility.ToJson(new Envelope {payload = payload, checksum = Hash(payload)});
                string temp = path + ".tmp";
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                { writer.Write(text); writer.Flush(); stream.Flush(true); }
                TideProfile previous;
                if (File.Exists(path) && TryRead(path, out previous))
                {
                    PreserveCorrupt(path + ".bak");
                    File.Replace(temp, path, path + ".bak");
                }
                else if (File.Exists(path))
                {
                    // A damaged primary must never replace the last verified backup.
                    // Supply a separate archive destination: replacing with a null
                    // backup can fail with ERROR_UNABLE_TO_REMOVE_REPLACED on Windows.
                    File.Replace(temp, path, CorruptPath(path));
                }
                else File.Move(temp, path);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { Diagnostic=ex.GetType().Name+" / "+ex.HResult+" / "+ex.Message;error = "저장하지 못했습니다. 진행을 종료하기 전에 다시 시도해 주세요."; return false; }
        }
    }
}
