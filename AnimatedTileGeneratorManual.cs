using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class AnimatedTileGeneratorManual : EditorWindow
{
    private Texture2D spriteSheet;
    private List<Sprite> selectedSprites = new List<Sprite>();
    private Vector2 scrollPosition;
    private float animationSpeed = 1f;
    private string tileName = "AnimatedTile";
    private string outputFolder = "Assets/Tiles";
    private Sprite[] allSprites;
    private bool showSpriteGrid = true;
    private int gridColumns = 8;
    private float spriteButtonSize = 60f;
    
    // Cache para os previews
    private Dictionary<Sprite, Texture2D> previewCache = new Dictionary<Sprite, Texture2D>();
    private Texture2D loadingTexture;

    [MenuItem("Tools/Animated Tile Generator (Manual)")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimatedTileGeneratorManual>("Animated Tile Generator Manual");
        window.minSize = new Vector2(500, 600);
    }

    private void OnEnable()
    {
        // Criar textura de loading simples
        loadingTexture = new Texture2D(2, 2);
        Color fillColor = new Color(0.6f, 0.6f, 0.6f);
        for (int i = 0; i < 4; i++)
            loadingTexture.SetPixel(i % 2, i / 2, fillColor);
        loadingTexture.Apply();
    }

    private void OnDisable()
    {
        previewCache.Clear();
    }

    private Texture2D GetSpritePreview(Sprite sprite)
    {
        if (sprite == null)
            return loadingTexture;

        // Se já tem no cache, retornar
        if (previewCache.ContainsKey(sprite) && previewCache[sprite] != null)
        {
            return previewCache[sprite];
        }

        // Tentar pegar o preview do AssetPreview
        Texture2D preview = AssetPreview.GetAssetPreview(sprite);
        
        if (preview != null && preview != loadingTexture)
        {
            previewCache[sprite] = preview;
            return preview;
        }

        // Se AssetPreview falhar, criar preview direto da textura do sprite
        if (sprite.texture != null)
        {
            try
            {
                // Criar um preview manualmente
                Texture2D customPreview = new Texture2D(
                    (int)sprite.rect.width, 
                    (int)sprite.rect.height,
                    TextureFormat.RGBA32,
                    false
                );
                
                // Copiar pixels do sprite
                Color[] pixels = sprite.texture.GetPixels(
                    (int)sprite.rect.x,
                    (int)sprite.rect.y,
                    (int)sprite.rect.width,
                    (int)sprite.rect.height
                );
                
                customPreview.SetPixels(pixels);
                customPreview.Apply();
                customPreview.filterMode = FilterMode.Point;
                
                previewCache[sprite] = customPreview;
                return customPreview;
            }
            catch
            {
                // Se falhar (textura não legível), usar AssetPreview
                if (preview != null)
                {
                    previewCache[sprite] = preview;
                    return preview;
                }
            }
        }

        return loadingTexture;
    }

    private void OnGUI()
    {
        GUILayout.Label("Gerador Manual de Animated Tiles", EditorStyles.boldLabel);
        GUILayout.Label("Selecione os sprites da animação na ordem correta", EditorStyles.miniLabel);
        
        EditorGUILayout.Space(10);

        // Seleção da sprite sheet
        EditorGUI.BeginChangeCheck();
        spriteSheet = (Texture2D)EditorGUILayout.ObjectField(
            "Sprite Sheet", 
            spriteSheet, 
            typeof(Texture2D), 
            false
        );
        
        if (EditorGUI.EndChangeCheck() && spriteSheet != null)
        {
            LoadSprites();
        }

        if (spriteSheet == null)
        {
            EditorGUILayout.HelpBox(
                "PASSO A PASSO:\n\n" +
                "1. Selecione sua Sprite Sheet acima\n" +
                "2. Clique em 'Carregar Todos os Previews'\n" +
                "3. Clique nos sprites NA ORDEM da animação\n" +
                "4. Configure nome e velocidade\n" +
                "5. Clique em 'Criar Animated Tile'\n\n" +
                "DICA: Sprites selecionados ficam com fundo verde!",
                MessageType.Info
            );
            return;
        }

        EditorGUILayout.Space(10);

        // Configurações
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Configurações", EditorStyles.boldLabel);
        
        tileName = EditorGUILayout.TextField("Nome do Tile", tileName);
        animationSpeed = EditorGUILayout.Slider("Velocidade", animationSpeed, 0.1f, 10f);
        outputFolder = EditorGUILayout.TextField("Pasta de Saída", outputFolder);
        
        EditorGUILayout.Space(5);
        gridColumns = EditorGUILayout.IntSlider("Colunas da Grade", gridColumns, 4, 16);
        spriteButtonSize = EditorGUILayout.Slider("Tamanho dos Botões", spriteButtonSize, 40f, 100f);
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Sprites selecionados
        DrawSelectedSprites();

        EditorGUILayout.Space(10);

        // Grade de sprites
        DrawSpriteGrid();

        EditorGUILayout.Space(10);

        // Botão de criar
        EditorGUI.BeginDisabledGroup(selectedSprites.Count < 2);
        
        if (GUILayout.Button("Criar Animated Tile", GUILayout.Height(40)))
        {
            CreateAnimatedTile();
        }
        
        EditorGUI.EndDisabledGroup();

        if (selectedSprites.Count < 2)
        {
            EditorGUILayout.HelpBox("Selecione pelo menos 2 sprites para criar uma animação", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"✓ {selectedSprites.Count} sprites selecionados e prontos!", MessageType.Info);
        }
    }

    private void DrawSelectedSprites()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label($"Sprites Selecionados: {selectedSprites.Count}", EditorStyles.boldLabel);
        
        if (selectedSprites.Count > 0)
        {
            int itemsPerRow = 8;
            int rows = Mathf.CeilToInt(selectedSprites.Count / (float)itemsPerRow);
            
            for (int row = 0; row < rows; row++)
            {
                GUILayout.BeginHorizontal();
                
                int startIndex = row * itemsPerRow;
                int endIndex = Mathf.Min(startIndex + itemsPerRow, selectedSprites.Count);
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (i < selectedSprites.Count)
                    {
                        DrawSelectedSpriteItem(i);
                    }
                }
                
                GUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Limpar Seleção"))
            {
                selectedSprites.Clear();
                Repaint();
            }
            
            if (GUILayout.Button("Remover Último"))
            {
                if (selectedSprites.Count > 0)
                {
                    selectedSprites.RemoveAt(selectedSprites.Count - 1);
                    Repaint();
                }
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Clique nos sprites abaixo para adicionar à animação", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedSpriteItem(int index)
    {
        if (index >= selectedSprites.Count || selectedSprites[index] == null)
            return;
            
        GUILayout.BeginVertical(GUILayout.Width(50));
        
        Texture2D preview = GetSpritePreview(selectedSprites[index]);
        
        Rect rect = GUILayoutUtility.GetRect(40, 40);
        if (preview != null)
        {
            GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
        }
        
        // Botão X para remover
        if (GUI.Button(new Rect(rect.x + rect.width - 16, rect.y, 16, 16), "X"))
        {
            selectedSprites.RemoveAt(index);
            GUILayout.EndVertical();
            Repaint();
            return;
        }
        
        GUILayout.Label($"#{index + 1}", EditorStyles.miniLabel);
        
        GUILayout.EndVertical();
    }

    private void DrawSpriteGrid()
    {
        if (allSprites == null || allSprites.Length == 0)
            return;

        EditorGUILayout.BeginVertical("box");
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Todos os Sprites ({allSprites.Length})", EditorStyles.boldLabel);
        showSpriteGrid = EditorGUILayout.Toggle("Mostrar", showSpriteGrid);
        
        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("Carregar Todos os Previews", GUILayout.Width(180), GUILayout.Height(25)))
        {
            LoadAllPreviews();
        }
        GUI.backgroundColor = oldColor;
        
        GUILayout.EndHorizontal();
        
        if (showSpriteGrid)
        {
            EditorGUILayout.Space(5);
            
            // Informação sobre previews
            int loadedPreviews = previewCache.Count;
            int totalSprites = allSprites.Length;
            
            if (loadedPreviews < totalSprites)
            {
                EditorGUILayout.HelpBox(
                    $"⚠ Previews carregados: {loadedPreviews}/{totalSprites}\n" +
                    "Clique no botão azul acima para carregar todos!",
                    MessageType.Warning
                );
            }
            else
            {
                EditorGUILayout.HelpBox($"✓ Todos os {totalSprites} previews carregados!", MessageType.Info);
            }
            
            // ScrollView
            scrollPosition = GUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.Height(300)
            );
            
            DrawSpriteGridContent();
            
            GUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawSpriteGridContent()
    {
        int columns = gridColumns;
        int rows = Mathf.CeilToInt(allSprites.Length / (float)columns);
        
        for (int row = 0; row < rows; row++)
        {
            GUILayout.BeginHorizontal();
            
            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                
                if (index >= allSprites.Length)
                {
                    GUILayout.EndHorizontal();
                    return;
                }
                
                DrawSpriteButton(index);
            }
            
            GUILayout.EndHorizontal();
        }
    }

    private void DrawSpriteButton(int index)
    {
        if (index >= allSprites.Length)
            return;
            
        Sprite sprite = allSprites[index];
        if (sprite == null)
            return;
            
        bool isSelected = selectedSprites.Contains(sprite);
        
        // Adicionar espaçamento entre os botões
        GUILayout.Space(2);
        GUILayout.BeginVertical(GUILayout.Width(spriteButtonSize + 8));
        
        Texture2D preview = GetSpritePreview(sprite);
        
        // Criar rect para o botão
        Rect buttonRect = GUILayoutUtility.GetRect(spriteButtonSize, spriteButtonSize);
        
        // Desenhar borda escura ao redor
        Rect borderRect = new Rect(buttonRect.x - 1, buttonRect.y - 1, buttonRect.width + 2, buttonRect.height + 2);
        EditorGUI.DrawRect(borderRect, new Color(0.2f, 0.2f, 0.2f, 1f));
        
        // Desenhar fundo
        Color backgroundColor = isSelected ? new Color(0.3f, 0.8f, 0.3f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
        EditorGUI.DrawRect(buttonRect, backgroundColor);
        
        // Criar área interna menor para o preview (com padding)
        Rect previewRect = new Rect(
            buttonRect.x + 2, 
            buttonRect.y + 2, 
            buttonRect.width - 4, 
            buttonRect.height - 4
        );
        
        // Desenhar o preview ESTICADO
        if (preview != null)
        {
            GUI.DrawTexture(previewRect, preview, ScaleMode.StretchToFill, true);
        }
        
        // Botão invisível por cima para detectar clique
        if (GUI.Button(buttonRect, "", GUIStyle.none))
        {
            if (isSelected)
            {
                selectedSprites.Remove(sprite);
            }
            else
            {
                selectedSprites.Add(sprite);
            }
            Repaint();
        }
        
        // Label
        string label = $"{index}";
        if (isSelected)
        {
            int position = selectedSprites.IndexOf(sprite) + 1;
            label += $" ✓#{position}";
        }
        
        GUILayout.Label(label, EditorStyles.miniLabel);
        
        GUILayout.EndVertical();
        GUILayout.Space(2);
    }

    private void LoadSprites()
    {
        if (spriteSheet == null)
            return;

        string path = AssetDatabase.GetAssetPath(spriteSheet);
        allSprites = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        selectedSprites.Clear();
        previewCache.Clear();

        if (allSprites.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Nenhum Sprite Encontrado",
                "A sprite sheet não contém sprites!\n\n" +
                "Certifique-se de:\n" +
                "1. Texture Type = Sprite (2D and UI)\n" +
                "2. Sprite Mode = Multiple\n" +
                "3. Usar o Sprite Editor para fatiar",
                "OK"
            );
        }
        else
        {
            Debug.Log($"✓ {allSprites.Length} sprites carregados da sprite sheet");
            Debug.Log("👉 Clique no botão 'Carregar Todos os Previews' para ver todas as imagens!");
        }
        
        Repaint();
    }

    private void LoadAllPreviews()
    {
        if (allSprites == null || allSprites.Length == 0)
            return;

        Debug.Log("Carregando todos os previews...");
        
        int loaded = 0;
        int failed = 0;
        
        for (int i = 0; i < allSprites.Length; i++)
        {
            if (allSprites[i] == null)
                continue;
                
            // Mostrar progresso
            if (i % 20 == 0)
            {
                EditorUtility.DisplayProgressBar(
                    "Carregando Previews", 
                    $"Processando sprite {i + 1}/{allSprites.Length}", 
                    (float)i / allSprites.Length
                );
            }
            
            // Pré-carregar o preview
            Texture2D preview = GetSpritePreview(allSprites[i]);
            
            if (preview != null && preview != loadingTexture)
            {
                loaded++;
            }
            else
            {
                failed++;
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        Debug.Log($"<color=green>✓ Previews carregados: {loaded} sucesso, {failed} falharam</color>");
        
        if (failed > 0)
        {
            Debug.LogWarning($"⚠ {failed} previews não puderam ser carregados. A textura pode não estar configurada como Read/Write enabled.");
        }
        
        Repaint();
    }

    private void CreateAnimatedTile()
    {
        if (selectedSprites.Count < 2)
        {
            EditorUtility.DisplayDialog("Erro", "Selecione pelo menos 2 sprites!", "OK");
            return;
        }

        // Criar pasta se não existir
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            string[] folders = outputFolder.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }

        // Criar o Animated Tile
        AnimatedTile animTile = ScriptableObject.CreateInstance<AnimatedTile>();
        animTile.m_AnimatedSprites = selectedSprites.ToArray();
        animTile.m_MinSpeed = animationSpeed;
        animTile.m_MaxSpeed = animationSpeed;

        // Salvar
        string assetPath = $"{outputFolder}/{tileName}.asset";
        
        // Se já existe, adicionar número
        int counter = 1;
        while (AssetDatabase.LoadAssetAtPath<AnimatedTile>(assetPath) != null)
        {
            assetPath = $"{outputFolder}/{tileName}_{counter}.asset";
            counter++;
        }

        AssetDatabase.CreateAsset(animTile, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "✓ Sucesso!", 
            $"Animated Tile criado com sucesso!\n\n" +
            $"📁 Arquivo: {assetPath}\n" +
            $"🎬 Frames: {selectedSprites.Count}\n" +
            $"⚡ Velocidade: {animationSpeed}x\n\n" +
            $"Agora arraste o tile para sua Tile Palette!",
            "OK"
        );

        // Limpar seleção
        selectedSprites.Clear();
        
        // Selecionar o tile criado
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimatedTile>(assetPath);
        EditorGUIUtility.PingObject(Selection.activeObject);

        Debug.Log($"<color=green>✓ Animated Tile criado: {assetPath}</color>");
        
        Repaint();
    }
}