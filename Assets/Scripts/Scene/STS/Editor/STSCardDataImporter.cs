using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fenêtre d'import : lit un dossier de JSON de cartes et génère les assets
/// STSCardData correspondants, rangés dans un sous-dossier par personnage favori.
/// </summary>
public class STSCardDataImporter : EditorWindow
{
    private const string DefaultTargetRoot = "Assets/Resources/STS/Cards";
    private const string SourcePrefKey = "STS.CardImporter.Source";
    private const string TargetPrefKey = "STS.CardImporter.Target";

    private string sourceFolder;
    private string targetRoot;
    private bool recursive = true;
    private bool overwriteExisting = true;
    private bool moveMisplacedAssets;
    private bool dryRun;

    private Vector2 logScroll;
    private readonly List<string> log = new();
    private string summary;

    [MenuItem("Tools/STS/Importer des cartes JSON")]
    public static void Open()
    {
        STSCardDataImporter window = GetWindow<STSCardDataImporter>("Import cartes STS");
        window.minSize = new Vector2(520f, 360f);
        window.Show();
    }

    private void OnEnable()
    {
        string defaultSource = Path.Combine(Application.dataPath, "StreamingAssets/STSCardData").Replace('\\', '/');
        sourceFolder = EditorPrefs.GetString(SourcePrefKey, defaultSource);
        targetRoot = EditorPrefs.GetString(TargetPrefKey, DefaultTargetRoot);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            sourceFolder = EditorGUILayout.TextField("Dossier de JSON", sourceFolder);
            if (GUILayout.Button("...", GUILayout.Width(30f)))
            {
                string start = Directory.Exists(sourceFolder) ? sourceFolder : Application.dataPath;
                string picked = EditorUtility.OpenFolderPanel("Dossier contenant les JSON de cartes", start, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    sourceFolder = picked.Replace('\\', '/');
                    GUI.FocusControl(null);
                }
            }
        }

        recursive = EditorGUILayout.Toggle("Sous-dossiers inclus", recursive);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Destination", EditorStyles.boldLabel);
        targetRoot = EditorGUILayout.TextField(
            new GUIContent("Dossier racine", "Chemin projet, ex. Assets/Resources/STS/Cards"),
            targetRoot);
        EditorGUILayout.HelpBox("Un sous-dossier est créé par personnage favori (EP, MECA, GM, ..., Aucun).", MessageType.None);

        overwriteExisting = EditorGUILayout.Toggle(
            new GUIContent("Écraser les assets existants", "Met à jour l'asset existant au lieu de l'ignorer. Son GUID est conservé, donc les références restent valides."),
            overwriteExisting);
        moveMisplacedAssets = EditorGUILayout.Toggle(
            new GUIContent("Déplacer les assets mal rangés", "Si une carte existe déjà ailleurs sous le dossier racine, la déplacer dans le sous-dossier de son personnage favori."),
            moveMisplacedAssets);
        dryRun = EditorGUILayout.Toggle(
            new GUIContent("Simulation", "N'écrit rien : liste seulement ce qui serait fait."),
            dryRun);

        EditorGUILayout.Space();
        bool invalid = string.IsNullOrWhiteSpace(sourceFolder) || string.IsNullOrWhiteSpace(targetRoot);
        using (new EditorGUI.DisabledScope(invalid))
        {
            if (GUILayout.Button(dryRun ? "Simuler l'import" : "Importer", GUILayout.Height(28f)))
            {
                EditorPrefs.SetString(SourcePrefKey, sourceFolder);
                EditorPrefs.SetString(TargetPrefKey, targetRoot);
                Import();
            }
        }

        if (!string.IsNullOrEmpty(summary))
        {
            EditorGUILayout.HelpBox(summary, MessageType.Info);
        }

        if (log.Count > 0)
        {
            EditorGUILayout.LabelField($"Détail ({log.Count})", EditorStyles.boldLabel);
            using (EditorGUILayout.ScrollViewScope scroll = new(logScroll))
            {
                logScroll = scroll.scrollPosition;
                foreach (string line in log)
                {
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }
    }

    private void Import()
    {
        log.Clear();
        summary = null;

        string source = sourceFolder.Replace('\\', '/').TrimEnd('/');
        string root = targetRoot.Replace('\\', '/').TrimEnd('/');

        if (!Directory.Exists(source))
        {
            summary = $"Dossier source introuvable : '{source}'.";
            return;
        }

        if (root != "Assets" && !root.StartsWith("Assets/", StringComparison.Ordinal))
        {
            summary = "Le dossier racine doit être un chemin projet commençant par 'Assets/'.";
            return;
        }

        SearchOption depth = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] files = Directory.GetFiles(source, "*.json", depth);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        List<CardSource> parsed = new();
        Dictionary<string, string> seenIds = new(StringComparer.Ordinal);
        int ignoredFiles = 0;

        foreach (string file in files)
        {
            if (!TryReadCards(file, out List<STSCardDataDTO> dtos, out string readError))
            {
                if (readError != null)
                {
                    log.Add($"[erreur] {ShortPath(file, source)} : {readError}");
                }
                else
                {
                    ignoredFiles++;
                }

                continue;
            }

            foreach (STSCardDataDTO dto in dtos)
            {
                Normalize(dto, file);
                if (string.IsNullOrWhiteSpace(dto.id))
                {
                    log.Add($"[erreur] {ShortPath(file, source)} : carte sans identifiant, ignorée.");
                    continue;
                }

                if (seenIds.TryGetValue(dto.id, out string firstFile))
                {
                    log.Add($"[doublon] '{dto.id}' déjà lu dans {ShortPath(firstFile, source)}, occurrence de {ShortPath(file, source)} ignorée.");
                    continue;
                }

                seenIds.Add(dto.id, file);
                parsed.Add(new CardSource { dto = dto, file = file });
            }
        }

        if (parsed.Count == 0)
        {
            summary = $"Aucune carte trouvée dans '{source}' ({files.Length} fichier(s) .json, {ignoredFiles} sans carte).";
            return;
        }

        Dictionary<string, string> existingByName = BuildExistingAssetPaths(root);

        if (!dryRun)
        {
            CreateCharacterFolders(root, parsed);
        }

        int created = 0;
        int updated = 0;
        int skipped = 0;
        int moved = 0;
        int failed = 0;

        try
        {
            for (int i = 0; i < parsed.Count; i++)
            {
                CardSource entry = parsed[i];
                if (EditorUtility.DisplayCancelableProgressBar("Import des cartes STS", entry.dto.id, (float)i / parsed.Count))
                {
                    log.Add("[annulé] Import interrompu.");
                    break;
                }

                STSCardData card;
                try
                {
                    card = STSCardData.FromDTO(entry.dto);
                }
                catch (Exception exception)
                {
                    failed++;
                    log.Add($"[erreur] '{entry.dto.id}' ({ShortPath(entry.file, source)}) : {exception.Message}");
                    continue;
                }

                string fileName = SanitizeFileName(entry.dto.id);
                string folder = $"{root}/{card.favoredCharacter}";
                string assetPath = $"{folder}/{fileName}.asset";

                string currentPath = null;
                if (existingByName.TryGetValue(fileName, out string knownPath))
                {
                    currentPath = knownPath;
                }
                else if (AssetDatabase.LoadAssetAtPath<STSCardData>(assetPath) != null)
                {
                    currentPath = assetPath;
                }

                if (currentPath != null && !overwriteExisting)
                {
                    skipped++;
                    log.Add($"[ignoré] '{entry.dto.id}' existe déjà ({currentPath}).");
                    DestroyImmediate(card);
                    continue;
                }

                if (dryRun)
                {
                    if (currentPath == null)
                    {
                        created++;
                        log.Add($"[créerait] {assetPath}");
                    }
                    else if (currentPath != assetPath && moveMisplacedAssets)
                    {
                        moved++;
                        updated++;
                        log.Add($"[déplacerait] {currentPath} -> {assetPath}");
                    }
                    else
                    {
                        updated++;
                        log.Add($"[mettrait à jour] {currentPath}");
                    }

                    DestroyImmediate(card);
                    continue;
                }

                EnsureFolderExists(folder);

                if (currentPath == null)
                {
                    card.name = fileName;
                    AssetDatabase.CreateAsset(card, assetPath);
                    existingByName[fileName] = assetPath;
                    created++;
                    log.Add($"[créé] {assetPath}");
                    continue;
                }

                if (currentPath != assetPath && moveMisplacedAssets)
                {
                    string moveError = AssetDatabase.MoveAsset(currentPath, assetPath);
                    if (string.IsNullOrEmpty(moveError))
                    {
                        log.Add($"[déplacé] {currentPath} -> {assetPath}");
                        currentPath = assetPath;
                        existingByName[fileName] = assetPath;
                        moved++;
                    }
                    else
                    {
                        log.Add($"[erreur] Déplacement de '{entry.dto.id}' impossible : {moveError}");
                    }
                }

                // Mise à jour en place plutôt que recréation : l'asset garde son GUID,
                // donc les références existantes (decks, ennemis, prefabs) restent valides.
                STSCardData existing = AssetDatabase.LoadAssetAtPath<STSCardData>(currentPath);
                if (existing == null)
                {
                    failed++;
                    log.Add($"[erreur] '{entry.dto.id}' : asset introuvable à '{currentPath}'.");
                    DestroyImmediate(card);
                    continue;
                }

                EditorUtility.CopySerialized(card, existing);
                existing.name = Path.GetFileNameWithoutExtension(currentPath);
                existing.id = existing.name;
                EditorUtility.SetDirty(existing);
                DestroyImmediate(card);
                updated++;
                log.Add($"[mis à jour] {currentPath}");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (!dryRun)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        StringBuilder builder = new();
        builder.Append(dryRun ? "Simulation : " : "Import terminé : ");
        builder.Append($"{created} créée(s), {updated} mise(s) à jour");
        if (moved > 0)
        {
            builder.Append($" (dont {moved} déplacée(s))");
        }

        builder.Append($", {skipped} ignorée(s), {failed} en erreur — sur {parsed.Count} carte(s) lues dans {files.Length} fichier(s).");
        summary = builder.ToString();
        Debug.Log(summary);
    }

    /// <summary>
    /// Accepte trois formes de fichier : une carte seule (absorption_de_choc.json),
    /// un tableau de cartes, ou une enveloppe <c>{ "cards": [...] }</c> (cards.json).
    /// Renvoie false sans erreur pour un JSON qui n'est pas une carte (index.json).
    /// </summary>
    private static bool TryReadCards(string file, out List<STSCardDataDTO> dtos, out string error)
    {
        dtos = null;
        error = null;

        JToken token;
        try
        {
            token = JToken.Parse(File.ReadAllText(file));
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }

        JToken cardsToken = token;
        if (token is JObject obj)
        {
            if (obj["cards"] is JArray wrapped)
            {
                cardsToken = wrapped;
            }
            else if (obj["id"] == null && obj["cardName"] == null)
            {
                return false;
            }
        }
        else if (!(token is JArray))
        {
            return false;
        }

        try
        {
            dtos = cardsToken is JArray array
                ? array.ToObject<List<STSCardDataDTO>>()
                : new List<STSCardDataDTO> { cardsToken.ToObject<STSCardDataDTO>() };
        }
        catch (JsonException exception)
        {
            error = exception.Message;
            return false;
        }

        dtos?.RemoveAll(dto => dto == null);
        return dtos != null && dtos.Count > 0;
    }

    /// <summary>
    /// Comble ce que <see cref="STSCardData.FromDTO"/> ne tolère pas : listes nulles
    /// et enums absents, qui feraient sinon échouer l'import de la carte entière.
    /// </summary>
    private static void Normalize(STSCardDataDTO dto, string file)
    {
        dto.effects ??= new List<EffectEntryDTO>();
        dto.modifiers ??= new List<ModifierDTO>();
        dto.tags ??= new List<string>();

        if (string.IsNullOrWhiteSpace(dto.id))
        {
            dto.id = !string.IsNullOrWhiteSpace(dto.cardName)
                ? dto.cardName
                : Path.GetFileNameWithoutExtension(file);
        }

        if (string.IsNullOrWhiteSpace(dto.type))
        {
            dto.type = CardType.Rien.ToString();
        }

        if (string.IsNullOrWhiteSpace(dto.rarity))
        {
            dto.rarity = CardRarity.Common.ToString();
        }

        if (string.IsNullOrWhiteSpace(dto.targetingMode))
        {
            dto.targetingMode = TargetingMode.Player.ToString();
        }

        if (string.IsNullOrWhiteSpace(dto.favoredCharacter))
        {
            dto.favoredCharacter = SelectableCharacter.Aucun.ToString();
        }

        if (dto.animationSpeed <= 0f)
        {
            dto.animationSpeed = 1f;
        }
    }

    /// <summary>
    /// Crée en une passe les sous-dossiers de personnages nécessaires, avant la
    /// boucle d'écriture : plus lisible dans la console qu'une création à la volée.
    /// </summary>
    private static void CreateCharacterFolders(string root, List<CardSource> cards)
    {
        EnsureFolderExists(root);

        HashSet<string> folders = new(StringComparer.Ordinal);
        foreach (CardSource entry in cards)
        {
            SelectableCharacter character = Enum.TryParse(entry.dto.favoredCharacter, out SelectableCharacter parsed)
                ? parsed
                : SelectableCharacter.Aucun;
            folders.Add(character.ToString());
        }

        foreach (string folder in folders)
        {
            EnsureFolderExists($"{root}/{folder}");
        }
    }

    private static Dictionary<string, string> BuildExistingAssetPaths(string root)
    {
        Dictionary<string, string> paths = new(StringComparer.Ordinal);
        if (!AssetDatabase.IsValidFolder(root))
        {
            return paths;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:STSCardData", new[] { root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            paths[Path.GetFileNameWithoutExtension(path)] = path;
        }

        return paths;
    }

    private static string SanitizeFileName(string raw)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        StringBuilder builder = new(raw.Length);
        foreach (char c in raw)
        {
            builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }

        return builder.ToString().Trim();
    }

    private static string ShortPath(string file, string source)
    {
        string normalized = file.Replace('\\', '/');
        return normalized.StartsWith(source, StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring(source.Length).TrimStart('/')
            : normalized;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    private struct CardSource
    {
        public STSCardDataDTO dto;
        public string file;
    }
}
