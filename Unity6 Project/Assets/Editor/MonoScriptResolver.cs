using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class MonoScriptResolver : EditorWindow
{
    private static readonly Regex ScriptRefRegex = new Regex(
        @"(m_Script:\s*\{fileID:\s*)(-?\d+)(,\s*guid:\s*)([a-f0-9]{32})(,\s*type:\s*\d+\})",
        RegexOptions.Compiled);

    private Vector2 _scroll;
    private Vector2 _scrollFingerprint;
    private bool _dryRun = true;
    private string _statusMessage = "";
    private List<RemapEntry> _remapEntries = new List<RemapEntry>();
    private List<FingerprintResult> _fingerprintResults = new List<FingerprintResult>();
    private bool _tableBuilt;
    private int _activeTab; // 0 = remap table, 1 = fingerprint

    private Dictionary<string, string> _dllGuids = new Dictionary<string, string>();
    private Dictionary<string, (string guid, long fileId)> _csScripts = new Dictionary<string, (string, long)>();

    // Candidate types for fingerprinting: typeName -> set of serialized field names
    private Dictionary<string, CandidateType> _candidateTypes = new Dictionary<string, CandidateType>();

    private class CandidateType
    {
        public string ClassName;
        public string Guid;
        public long FileId;
        public HashSet<string> FieldNames = new HashSet<string>();
    }

    private class RemapEntry
    {
        public string TypeName;
        public string OldGuid;
        public long OldFileId;
        public string NewGuid;
        public long NewFileId;
        public string SourceDll;
        public bool Selected = true;
        public int HitCount;
    }

    private class FingerprintResult
    {
        public string FilePath;
        public int LineNumber;
        public string OldGuid;
        public long OldFileId;
        public List<string> YamlFields = new List<string>();
        public string BestMatchType;
        public string BestMatchGuid;
        public long BestMatchFileId;
        public int MatchedCount;
        public int TotalYamlFields;
        public int TotalCandidateFields;
        public List<string> MissingFromCandidate = new List<string>();
        public List<string> ExtraInCandidate = new List<string>();
        public float Score;
        public bool Selected = true;
        public bool Expanded;
    }

    [MenuItem("Tools/MonoScript Resolver")]
    public static void ShowWindow()
    {
        var window = GetWindow<MonoScriptResolver>("MonoScript Resolver");
        window.minSize = new Vector2(800, 600);
        window.Show();
    }

    private void OnEnable()
    {
        BuildMappingTable();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("MonoScript Resolver", EditorStyles.boldLabel);

        EditorGUILayout.Space(4);
        _activeTab = GUILayout.Toolbar(_activeTab, new[] { "DLL → .cs Remap", "Field Fingerprint (Unresolved)" });

        EditorGUILayout.Space(4);

        if (_activeTab == 0)
            DrawRemapTab();
        else
            DrawFingerprintTab();
    }

    // =====================================================================
    // TAB 0: DLL -> .cs Remap (existing functionality)
    // =====================================================================
    private void DrawRemapTab()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1. Rebuild Mapping Table", GUILayout.Height(26)))
            BuildMappingTable();
        if (GUILayout.Button("2. Scan (Dry Run)", GUILayout.Height(26)))
        { _dryRun = true; ScanAndFix(); }
        if (GUILayout.Button("3. Apply Fixes", GUILayout.Height(26)))
        { _dryRun = false; ScanAndFix(); }
        EditorGUILayout.EndHorizontal();

        _dryRun = EditorGUILayout.ToggleLeft("Dry Run", _dryRun);

        if (!string.IsNullOrEmpty(_statusMessage))
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);

        if (_remapEntries.Count > 0)
        {
            EditorGUILayout.LabelField($"Remap Table ({_remapEntries.Count} entries):", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80))) _remapEntries.ForEach(r => r.Selected = true);
            if (GUILayout.Button("Select None", GUILayout.Width(80))) _remapEntries.ForEach(r => r.Selected = false);
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("", GUILayout.Width(18));
            EditorGUILayout.LabelField("Type", EditorStyles.miniLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField("DLL", EditorStyles.miniLabel, GUILayout.Width(130));
            EditorGUILayout.LabelField("Old FileID", EditorStyles.miniLabel, GUILayout.Width(110));
            EditorGUILayout.LabelField("→ .cs GUID", EditorStyles.miniLabel, GUILayout.Width(240));
            EditorGUILayout.LabelField("Hits", EditorStyles.miniLabel, GUILayout.Width(35));
            EditorGUILayout.EndHorizontal();

            foreach (var entry in _remapEntries)
            {
                EditorGUILayout.BeginHorizontal();
                entry.Selected = EditorGUILayout.Toggle(entry.Selected, GUILayout.Width(18));
                EditorGUILayout.LabelField(entry.TypeName, GUILayout.Width(180));
                EditorGUILayout.LabelField(entry.SourceDll, GUILayout.Width(130));
                EditorGUILayout.LabelField(entry.OldFileId.ToString(), GUILayout.Width(110));
                EditorGUILayout.LabelField(entry.NewGuid, GUILayout.Width(240));
                EditorGUILayout.LabelField(entry.HitCount.ToString(), GUILayout.Width(35));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
        else if (_tableBuilt)
        {
            EditorGUILayout.HelpBox("No remap entries. Check Console for details.", MessageType.Warning);
        }
    }

    // =====================================================================
    // TAB 1: Field Fingerprinting
    // =====================================================================
    private void DrawFingerprintTab()
    {
        EditorGUILayout.HelpBox(
            "Scans prefabs for broken m_Script references, reads the serialized fields from YAML,\n" +
            "and matches them against all available MonoBehaviour types by field name overlap.\n" +
            "Shows: matched X / Y fields, missing fields, extra fields.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan & Fingerprint", GUILayout.Height(28)))
            ScanAndFingerprint();
        if (_fingerprintResults.Count > 0 && GUILayout.Button("Apply Selected", GUILayout.Height(28)))
            ApplyFingerprintFixes();
        EditorGUILayout.EndHorizontal();

        _dryRun = EditorGUILayout.ToggleLeft("Dry Run", _dryRun);

        if (_fingerprintResults.Count > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Unresolved References: {_fingerprintResults.Count}", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80))) _fingerprintResults.ForEach(r => r.Selected = true);
            if (GUILayout.Button("Select None", GUILayout.Width(80))) _fingerprintResults.ForEach(r => r.Selected = false);
            EditorGUILayout.EndHorizontal();

            _scrollFingerprint = EditorGUILayout.BeginScrollView(_scrollFingerprint);

            foreach (var result in _fingerprintResults)
            {
                EditorGUILayout.BeginVertical("box");

                // Header row
                EditorGUILayout.BeginHorizontal();
                result.Selected = EditorGUILayout.Toggle(result.Selected, GUILayout.Width(18));

                string fileName = Path.GetFileName(result.FilePath);
                string matchLabel = result.BestMatchType != null
                    ? $"→ {result.BestMatchType}  ({result.MatchedCount}/{result.TotalYamlFields} fields match)"
                    : "NO MATCH";

                Color oldColor = GUI.color;
                if (result.Score >= 0.8f) GUI.color = Color.green;
                else if (result.Score >= 0.5f) GUI.color = Color.yellow;
                else GUI.color = new Color(1f, 0.5f, 0.5f);

                EditorGUILayout.LabelField($"{fileName}:{result.LineNumber}", EditorStyles.boldLabel, GUILayout.Width(220));
                EditorGUILayout.LabelField(matchLabel, GUILayout.Width(350));
                GUI.color = oldColor;

                result.Expanded = EditorGUILayout.Foldout(result.Expanded, "Details", true);
                EditorGUILayout.EndHorizontal();

                // Expanded details
                if (result.Expanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"Old GUID: {result.OldGuid}   FileID: {result.OldFileId}");
                    EditorGUILayout.LabelField($"Score: {result.Score:P0}  ({result.MatchedCount} matched / {result.TotalYamlFields} YAML fields / {result.TotalCandidateFields} candidate fields)");

                    if (result.MissingFromCandidate.Count > 0)
                    {
                        EditorGUILayout.LabelField($"Fields in YAML but NOT in candidate ({result.MissingFromCandidate.Count}):",
                            EditorStyles.miniLabel);
                        EditorGUILayout.LabelField("    " + string.Join(", ", result.MissingFromCandidate),
                            EditorStyles.wordWrappedMiniLabel);
                    }

                    if (result.ExtraInCandidate.Count > 0)
                    {
                        EditorGUILayout.LabelField($"Fields in candidate but NOT in YAML ({result.ExtraInCandidate.Count}):",
                            EditorStyles.miniLabel);
                        EditorGUILayout.LabelField("    " + string.Join(", ", result.ExtraInCandidate),
                            EditorStyles.wordWrappedMiniLabel);
                    }

                    if (result.YamlFields.Count > 0)
                    {
                        EditorGUILayout.LabelField("All YAML fields:", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField("    " + string.Join(", ", result.YamlFields),
                            EditorStyles.wordWrappedMiniLabel);
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    // =====================================================================
    // Fingerprint scanning
    // =====================================================================
    private void ScanAndFingerprint()
    {
        _fingerprintResults.Clear();
        BuildCandidateTypes();

        // Collect all known/resolvable GUIDs
        HashSet<string> knownGuids = new HashSet<string>();
        foreach (var cs in _csScripts.Values) knownGuids.Add(cs.guid);
        // Add any guid that resolves via AssetDatabase
        string[] allMonoGuids = AssetDatabase.FindAssets("t:MonoScript");
        foreach (string g in allMonoGuids) knownGuids.Add(g);

        string assetsPath = Application.dataPath;
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(assetsPath, "*.prefab", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(assetsPath, "*.asset", SearchOption.AllDirectories));

        for (int fi = 0; fi < files.Count; fi++)
        {
            if (fi % 20 == 0)
                EditorUtility.DisplayProgressBar("Fingerprinting", $"{fi}/{files.Count}", (float)fi / files.Count);

            try { ParseFileForBrokenScripts(files[fi], knownGuids); }
            catch (Exception ex) { Debug.LogWarning($"[MonoScriptResolver] {files[fi]}: {ex.Message}"); }
        }

        EditorUtility.ClearProgressBar();

        // Now fingerprint each result
        foreach (var result in _fingerprintResults)
        {
            MatchFieldsToCandidate(result);
        }

        // Sort by score descending
        _fingerprintResults.Sort((a, b) => b.Score.CompareTo(a.Score));

        _statusMessage = $"Found {_fingerprintResults.Count} unresolved references. {_candidateTypes.Count} candidate types available.";
        Repaint();
    }

    private void ParseFileForBrokenScripts(string filePath, HashSet<string> knownGuids)
    {
        string[] lines = File.ReadAllLines(filePath);

        int blockStart = -1;
        string blockGuid = null;
        long blockFileId = 0;
        int blockScriptLine = -1;
        List<string> blockFields = new List<string>();
        bool isBroken = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.StartsWith("---"))
            {
                // Flush previous block
                if (isBroken && blockGuid != null && blockFields.Count > 0)
                {
                    _fingerprintResults.Add(new FingerprintResult
                    {
                        FilePath = filePath,
                        LineNumber = blockScriptLine + 1,
                        OldGuid = blockGuid,
                        OldFileId = blockFileId,
                        YamlFields = new List<string>(blockFields)
                    });
                }

                blockStart = -1;
                blockGuid = null;
                blockFileId = 0;
                blockScriptLine = -1;
                blockFields.Clear();
                isBroken = false;

                if (line.Contains("!u!114"))
                    blockStart = i;

                continue;
            }

            if (blockStart < 0) continue;

            Match m = ScriptRefRegex.Match(line);
            if (m.Success)
            {
                string guid = m.Groups[4].Value;
                long fileId = long.Parse(m.Groups[2].Value);

                // Check if this resolves
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                bool resolves = !string.IsNullOrEmpty(assetPath);

                // Also check if the actual MonoScript inside can be loaded
                if (resolves)
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (obj == null) resolves = false;
                }

                if (!resolves && !knownGuids.Contains(guid))
                {
                    isBroken = true;
                    blockGuid = guid;
                    blockFileId = fileId;
                    blockScriptLine = i;
                }
                continue;
            }

            // Collect serialized field names from YAML
            if (isBroken && line.Length > 2 && line[0] == ' ' && line[1] == ' ' && !line.StartsWith("  -"))
            {
                string trimmed = line.TrimStart();
                int colonIdx = trimmed.IndexOf(':');
                if (colonIdx > 0)
                {
                    string fieldName = trimmed.Substring(0, colonIdx);
                    // Include both m_ prefixed and non-prefixed (exclude m_ObjectHideFlags etc. which are Unity internal)
                    if (!IsUnityInternalField(fieldName))
                    {
                        blockFields.Add(fieldName);
                    }
                }
            }
        }

        // Last block
        if (isBroken && blockGuid != null && blockFields.Count > 0)
        {
            _fingerprintResults.Add(new FingerprintResult
            {
                FilePath = filePath,
                LineNumber = blockScriptLine + 1,
                OldGuid = blockGuid,
                OldFileId = blockFileId,
                YamlFields = new List<string>(blockFields)
            });
        }
    }

    private static bool IsUnityInternalField(string name)
    {
        // Unity's built-in MonoBehaviour serialized fields
        return name == "m_ObjectHideFlags" || name == "m_CorrespondingSourceObject" ||
               name == "m_PrefabInstance" || name == "m_PrefabAsset" || name == "m_GameObject" ||
               name == "m_Enabled" || name == "m_EditorHideFlags" || name == "m_Script" ||
               name == "m_Name" || name == "m_EditorClassIdentifier";
    }

    private void MatchFieldsToCandidate(FingerprintResult result)
    {
        if (result.YamlFields.Count == 0) return;

        HashSet<string> yamlFields = new HashSet<string>(result.YamlFields);

        string bestType = null;
        string bestGuid = null;
        long bestFileId = 0;
        int bestMatched = 0;
        int bestCandidateTotal = 0;
        float bestScore = 0;
        List<string> bestMissing = null;
        List<string> bestExtra = null;

        foreach (var kvp in _candidateTypes)
        {
            var candidate = kvp.Value;
            if (candidate.FieldNames.Count == 0) continue;

            int matched = 0;
            List<string> missing = new List<string>();
            List<string> extra = new List<string>();

            foreach (string f in yamlFields)
            {
                if (candidate.FieldNames.Contains(f))
                    matched++;
                else
                    missing.Add(f);
            }

            foreach (string f in candidate.FieldNames)
            {
                if (!yamlFields.Contains(f))
                    extra.Add(f);
            }

            // Score: matched / max(yaml count, candidate count)
            float score = (float)matched / Mathf.Max(yamlFields.Count, candidate.FieldNames.Count);

            if (score > bestScore || (score == bestScore && matched > bestMatched))
            {
                bestScore = score;
                bestType = candidate.ClassName;
                bestGuid = candidate.Guid;
                bestFileId = candidate.FileId;
                bestMatched = matched;
                bestCandidateTotal = candidate.FieldNames.Count;
                bestMissing = missing;
                bestExtra = extra;
            }
        }

        result.BestMatchType = bestType;
        result.BestMatchGuid = bestGuid;
        result.BestMatchFileId = bestFileId;
        result.MatchedCount = bestMatched;
        result.TotalYamlFields = yamlFields.Count;
        result.TotalCandidateFields = bestCandidateTotal;
        result.MissingFromCandidate = bestMissing ?? new List<string>();
        result.ExtraInCandidate = bestExtra ?? new List<string>();
        result.Score = bestScore;
    }

    private void BuildCandidateTypes()
    {
        _candidateTypes.Clear();

        // First re-index .cs scripts
        if (_csScripts.Count == 0) BuildMappingTable();

        // Get ALL MonoScript assets and build field lists via reflection
        string[] guids = AssetDatabase.FindAssets("t:MonoScript");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (ms == null) continue;

            Type t = ms.GetClass();
            if (t == null) continue;

            if (!typeof(MonoBehaviour).IsAssignableFrom(t) &&
                !typeof(ScriptableObject).IsAssignableFrom(t))
                continue;

            string className = t.Name;

            long fileId = 11500000; // default for .cs
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ms, out _, out long localId))
                    fileId = localId;
            }

            var candidate = new CandidateType
            {
                ClassName = className,
                Guid = guid,
                FileId = fileId,
                FieldNames = GetSerializedFieldNames(t)
            };

            _candidateTypes[className] = candidate;
        }

        // Also add from .cs scripts whose types couldn't be loaded (use filename)
        string[] csGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" });
        foreach (string guid in csGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".cs")) continue;
            MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (ms == null) continue;
            Type t = ms.GetClass();
            if (t != null && !_candidateTypes.ContainsKey(t.Name))
            {
                _candidateTypes[t.Name] = new CandidateType
                {
                    ClassName = t.Name,
                    Guid = guid,
                    FileId = 11500000,
                    FieldNames = GetSerializedFieldNames(t)
                };
            }
        }

        Debug.Log($"[MonoScriptResolver] Built {_candidateTypes.Count} candidate types for fingerprinting.");
    }

    private static HashSet<string> GetSerializedFieldNames(Type type)
    {
        var fields = new HashSet<string>();
        if (type == null) return fields;

        try
        {
            Type current = type;
            while (current != null &&
                   current != typeof(MonoBehaviour) &&
                   current != typeof(ScriptableObject) &&
                   current != typeof(UnityEngine.Object) &&
                   current != typeof(object))
            {
                FieldInfo[] allFields = current.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (FieldInfo fi in allFields)
                {
                    bool isPublic = fi.IsPublic;
                    bool hasSerializeField = fi.GetCustomAttribute<SerializeField>() != null;
                    bool hasNonSerialized = fi.GetCustomAttribute<NonSerializedAttribute>() != null;

                    if (hasNonSerialized) continue;
                    if (isPublic || hasSerializeField)
                    {
                        fields.Add(fi.Name);
                    }
                }

                current = current.BaseType;
            }
        }
        catch { }

        return fields;
    }

    private void ApplyFingerprintFixes()
    {
        var toFix = _fingerprintResults.Where(r => r.Selected && r.BestMatchGuid != null && r.Score > 0.3f).ToList();
        if (toFix.Count == 0)
        {
            _statusMessage = "No fixable entries selected (need score > 30%).";
            return;
        }

        int totalFixed = 0;
        var byFile = toFix.GroupBy(r => r.FilePath);
        StringBuilder log = new StringBuilder();

        foreach (var group in byFile)
        {
            string[] lines = File.ReadAllLines(group.Key);
            bool modified = false;

            foreach (var result in group)
            {
                int lineIdx = result.LineNumber - 1;
                if (lineIdx < 0 || lineIdx >= lines.Length) continue;

                Match m = ScriptRefRegex.Match(lines[lineIdx]);
                if (!m.Success) continue;

                string newLine = $"{m.Groups[1].Value}{result.BestMatchFileId}{m.Groups[3].Value}{result.BestMatchGuid}{m.Groups[5].Value}";

                if (_dryRun)
                {
                    log.AppendLine($"  {Path.GetFileName(result.FilePath)}:{result.LineNumber} → {result.BestMatchType} ({result.MatchedCount}/{result.TotalYamlFields} fields)");
                }
                else
                {
                    lines[lineIdx] = newLine;
                    modified = true;
                }
                totalFixed++;
            }

            if (modified)
            {
                File.WriteAllLines(group.Key, lines);
            }
        }

        if (_dryRun)
        {
            _statusMessage = $"[DRY RUN] Would fix {totalFixed} references:\n{log}";
        }
        else
        {
            _statusMessage = $"Fixed {totalFixed} references.";
            AssetDatabase.Refresh();
            ScanAndFingerprint(); // re-scan
        }

        Debug.Log($"[MonoScriptResolver] {_statusMessage}");
        Repaint();
    }

    // =====================================================================
    // DLL -> .cs Remap (existing logic)
    // =====================================================================
    private static long ComputeFileIdForType(string namespaceName, string className)
    {
        string fullName = "s\0\0\0" + namespaceName + className;
        byte[] hash = ComputeMD4(Encoding.UTF8.GetBytes(fullName));
        int id = BitConverter.ToInt32(hash, 0);
        if (id == 0) id = 1;
        return id;
    }

    private static byte[] ComputeMD4(byte[] input)
    {
        uint a = 0x67452301, b = 0xefcdab89, c = 0x98badcfe, d = 0x10325476;
        int originalLength = input.Length;
        int paddedLength = ((originalLength + 8) / 64 + 1) * 64;
        byte[] padded = new byte[paddedLength];
        Array.Copy(input, padded, originalLength);
        padded[originalLength] = 0x80;
        long bitLength = (long)originalLength * 8;
        Array.Copy(BitConverter.GetBytes(bitLength), 0, padded, paddedLength - 8, 8);

        for (int i = 0; i < paddedLength; i += 64)
        {
            uint[] x = new uint[16];
            for (int j = 0; j < 16; j++) x[j] = BitConverter.ToUInt32(padded, i + j * 4);
            uint aa = a, bb = b, cc = c, dd = d;

            a = R1(a,b,c,d,x[0],3);d=R1(d,a,b,c,x[1],7);c=R1(c,d,a,b,x[2],11);b=R1(b,c,d,a,x[3],19);
            a = R1(a,b,c,d,x[4],3);d=R1(d,a,b,c,x[5],7);c=R1(c,d,a,b,x[6],11);b=R1(b,c,d,a,x[7],19);
            a = R1(a,b,c,d,x[8],3);d=R1(d,a,b,c,x[9],7);c=R1(c,d,a,b,x[10],11);b=R1(b,c,d,a,x[11],19);
            a = R1(a,b,c,d,x[12],3);d=R1(d,a,b,c,x[13],7);c=R1(c,d,a,b,x[14],11);b=R1(b,c,d,a,x[15],19);

            a = R2(a,b,c,d,x[0],3);d=R2(d,a,b,c,x[4],5);c=R2(c,d,a,b,x[8],9);b=R2(b,c,d,a,x[12],13);
            a = R2(a,b,c,d,x[1],3);d=R2(d,a,b,c,x[5],5);c=R2(c,d,a,b,x[9],9);b=R2(b,c,d,a,x[13],13);
            a = R2(a,b,c,d,x[2],3);d=R2(d,a,b,c,x[6],5);c=R2(c,d,a,b,x[10],9);b=R2(b,c,d,a,x[14],13);
            a = R2(a,b,c,d,x[3],3);d=R2(d,a,b,c,x[7],5);c=R2(c,d,a,b,x[11],9);b=R2(b,c,d,a,x[15],13);

            a = R3(a,b,c,d,x[0],3);d=R3(d,a,b,c,x[8],9);c=R3(c,d,a,b,x[4],11);b=R3(b,c,d,a,x[12],15);
            a = R3(a,b,c,d,x[2],3);d=R3(d,a,b,c,x[10],9);c=R3(c,d,a,b,x[6],11);b=R3(b,c,d,a,x[14],15);
            a = R3(a,b,c,d,x[1],3);d=R3(d,a,b,c,x[9],9);c=R3(c,d,a,b,x[5],11);b=R3(b,c,d,a,x[13],15);
            a = R3(a,b,c,d,x[3],3);d=R3(d,a,b,c,x[11],9);c=R3(c,d,a,b,x[7],11);b=R3(b,c,d,a,x[15],15);

            a += aa; b += bb; c += cc; d += dd;
        }

        byte[] result = new byte[16];
        Array.Copy(BitConverter.GetBytes(a), 0, result, 0, 4);
        Array.Copy(BitConverter.GetBytes(b), 0, result, 4, 4);
        Array.Copy(BitConverter.GetBytes(c), 0, result, 8, 4);
        Array.Copy(BitConverter.GetBytes(d), 0, result, 12, 4);
        return result;
    }

    private static uint RL(uint x, int n) => (x << n) | (x >> (32 - n));
    private static uint R1(uint a, uint b, uint c, uint d, uint x, int s) => RL(a + ((b & c) | (~b & d)) + x, s);
    private static uint R2(uint a, uint b, uint c, uint d, uint x, int s) => RL(a + ((b & c) | (b & d) | (c & d)) + x + 0x5a827999u, s);
    private static uint R3(uint a, uint b, uint c, uint d, uint x, int s) => RL(a + (b ^ c ^ d) + x + 0x6ed9eba1u, s);

    private void BuildMappingTable()
    {
        _remapEntries.Clear();
        _dllGuids.Clear();
        _csScripts.Clear();

        string[] dllFiles = Directory.GetFiles(Application.dataPath, "*.dll", SearchOption.AllDirectories);
        foreach (string dllPath in dllFiles)
        {
            string relativePath = "Assets" + dllPath.Substring(Application.dataPath.Length).Replace('\\', '/');
            string guid = AssetDatabase.AssetPathToGUID(relativePath);
            if (!string.IsNullOrEmpty(guid))
            {
                _dllGuids[guid] = Path.GetFileName(dllPath);
                Debug.Log($"[MonoScriptResolver] DLL: {Path.GetFileName(dllPath)} GUID={guid}");
            }
        }

        string[] csGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" });
        foreach (string guid in csGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".cs")) continue;

            MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (ms == null) continue;

            Type t = ms.GetClass();
            if (t == null)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                _csScripts[fileName] = (guid, 11500000);
                continue;
            }

            _csScripts[t.Name] = (guid, 11500000);
            if (t.FullName != null && t.FullName != t.Name)
                _csScripts[t.FullName] = (guid, 11500000);
        }

        foreach (var dllKvp in _dllGuids)
        {
            string dllGuid = dllKvp.Key;
            string dllName = dllKvp.Value;
            string dllPath = AssetDatabase.GUIDToAssetPath(dllGuid);

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(dllPath);
            if (allAssets == null) continue;

            foreach (var asset in allAssets)
            {
                MonoScript ms = asset as MonoScript;
                if (ms == null) continue;
                Type t = ms.GetClass();
                if (t == null) continue;
                if (!typeof(MonoBehaviour).IsAssignableFrom(t) && !typeof(ScriptableObject).IsAssignableFrom(t)) continue;

                long fileId = 0;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ms, out _, out long localId))
                    fileId = localId;
                else
                    fileId = ComputeFileIdForType(t.Namespace ?? "", t.Name);

                if (_csScripts.TryGetValue(t.Name, out var csTarget) ||
                    _csScripts.TryGetValue(t.FullName ?? t.Name, out csTarget))
                {
                    _remapEntries.Add(new RemapEntry
                    {
                        TypeName = t.Name,
                        OldGuid = dllGuid,
                        OldFileId = fileId,
                        NewGuid = csTarget.guid,
                        NewFileId = csTarget.fileId,
                        SourceDll = dllName,
                        HitCount = 0
                    });
                }
            }
        }

        if (_remapEntries.Count == 0)
        {
            foreach (var csKvp in _csScripts)
            {
                if (csKvp.Key.Contains('.')) continue;
                foreach (var dllKvp in _dllGuids)
                {
                    long fileIdNoNs = ComputeFileIdForType("", csKvp.Key);
                    _remapEntries.Add(new RemapEntry
                    {
                        TypeName = csKvp.Key,
                        OldGuid = dllKvp.Key,
                        OldFileId = fileIdNoNs,
                        NewGuid = csKvp.Value.guid,
                        NewFileId = csKvp.Value.fileId,
                        SourceDll = dllKvp.Value,
                        HitCount = 0
                    });
                }
            }
        }

        _tableBuilt = true;
        _statusMessage = $"{_dllGuids.Count} DLLs, {_csScripts.Count} .cs scripts, {_remapEntries.Count} remap entries.";
        Debug.Log($"[MonoScriptResolver] {_statusMessage}");
        Repaint();
    }

    private void ScanAndFix()
    {
        if (_remapEntries.Count == 0) { _statusMessage = "No remap entries."; return; }

        var activeRemap = new Dictionary<(string guid, long fileId), RemapEntry>();
        foreach (var entry in _remapEntries.Where(e => e.Selected))
        {
            var key = (entry.OldGuid, entry.OldFileId);
            if (!activeRemap.ContainsKey(key)) activeRemap[key] = entry;
        }

        HashSet<string> targetGuids = new HashSet<string>(activeRemap.Keys.Select(k => k.guid));
        string assetsPath = Application.dataPath;
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(assetsPath, "*.prefab", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(assetsPath, "*.asset", SearchOption.AllDirectories));

        int totalFixed = 0, filesModified = 0, unmatched = 0;
        StringBuilder log = new StringBuilder();

        for (int fi = 0; fi < files.Count; fi++)
        {
            if (fi % 20 == 0) EditorUtility.DisplayProgressBar("Scanning", $"{fi}/{files.Count}", (float)fi / files.Count);

            try
            {
                string content = File.ReadAllText(files[fi]);
                bool hasTarget = targetGuids.Any(g => content.Contains(g));
                if (!hasTarget) continue;

                string[] lines = File.ReadAllLines(files[fi]);
                bool modified = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    Match m = ScriptRefRegex.Match(lines[i]);
                    if (!m.Success) continue;
                    long fileId = long.Parse(m.Groups[2].Value);
                    string guid = m.Groups[4].Value;
                    if (!targetGuids.Contains(guid)) continue;

                    if (activeRemap.TryGetValue((guid, fileId), out var entry))
                    {
                        lines[i] = $"{m.Groups[1].Value}{entry.NewFileId}{m.Groups[3].Value}{entry.NewGuid}{m.Groups[5].Value}";
                        modified = true; totalFixed++; entry.HitCount++;
                        log.AppendLine($"  {Path.GetFileName(files[fi])}:{i+1} → {entry.TypeName}");
                    }
                    else { unmatched++; }
                }

                if (modified && !_dryRun) { File.WriteAllLines(files[fi], lines); filesModified++; }
                else if (modified) { filesModified++; }
            }
            catch (Exception ex) { Debug.LogWarning($"[MonoScriptResolver] {ex.Message}"); }
        }

        EditorUtility.ClearProgressBar();
        string prefix = _dryRun ? "[DRY RUN] " : "";
        _statusMessage = $"{prefix}{totalFixed} fixed, {filesModified} files, {unmatched} unmatched.\n{log}";
        if (!_dryRun && totalFixed > 0) AssetDatabase.Refresh();
        Repaint();
    }
}
