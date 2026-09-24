using UnityEngine;
using UnityEditor;
using System.IO;

public class CorrigirMateriaisURP
{
    [MenuItem("Tools/URP/Corrigir Materiais Automático")]
    [MenuItem("Assets/URP/Corrigir Materiais Automático")]
    public static void CorrigirAutomatico()
    {
        Object[] selecionados = Selection.objects;

        if (selecionados == null || selecionados.Length == 0)
        {
            EditorUtility.DisplayDialog("Aviso", "Por favor, selecione a pasta de materiais ou os materiais na janela Project.", "OK");
            return;
        }

        Shader shaderURP = Shader.Find("Universal Render Pipeline/Lit");

        if (shaderURP == null)
        {
            Debug.LogError("[URP] Shader 'Universal Render Pipeline/Lit' não foi encontrado no projeto.");
            return;
        }

        int corrigidos = 0;

        for (int i = 0; i < selecionados.Length; i++)
        {
            Object obj = selecionados[i];

            if (obj is Material)
            {
                Material mat = (Material)obj;
                ProcessarMaterialAutomatico(mat, shaderURP);
                corrigidos++;
            }
            else if (obj is DefaultAsset) // Se selecionou a pasta
            {
                string pastaPath = AssetDatabase.GetAssetPath(obj);
                string[] guids = AssetDatabase.FindAssets("t:Material", new string[] { pastaPath });

                for (int j = 0; j < guids.Length; j++)
                {
                    string matPath = AssetDatabase.GUIDToAssetPath(guids[j]);
                    Material mat = (Material)AssetDatabase.LoadAssetAtPath(matPath, typeof(Material));
                    if (mat != null)
                    {
                        ProcessarMaterialAutomatico(mat, shaderURP);
                        corrigidos++;
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[URP] Processo Automático Concluído! " + corrigidos + " materiais foram corrigidos.");
    }

    private static void ProcessarMaterialAutomatico(Material mat, Shader shaderURP)
    {
        // 1. Tenta resgatar a textura que já estava no material
        Texture texturaEncontrada = mat.mainTexture;

        if (texturaEncontrada == null && mat.HasProperty("_MainTex"))
        {
            texturaEncontrada = mat.GetTexture("_MainTex");
        }

        // 2. Se o material estiver sem textura, procura automaticamente na pasta do pacote
        if (texturaEncontrada == null)
        {
            texturaEncontrada = LocalizarTexturaDoPacote(mat);
        }

        // 3. Aplica o Shader URP Lit
        mat.shader = shaderURP;

        // 4. Atribui a textura encontrada ao Base Map
        if (texturaEncontrada != null)
        {
            mat.SetTexture("_BaseMap", texturaEncontrada);
        }

        mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
    }

    private static Texture LocalizarTexturaDoPacote(Material mat)
    {
        string matPath = AssetDatabase.GetAssetPath(mat);
        string pastaMateriais = Path.GetDirectoryName(matPath);
        string pastaFBX = Path.GetDirectoryName(pastaMateriais);
        string pastaRaizPacote = Path.GetDirectoryName(pastaFBX); // Pasta raiz (ex: Town Creator Kit LITE)

        // Procura todas as texturas dentro do pacote do asset
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { pastaRaizPacote });

        // Prioridade A: Procura por nome equivalente (ex: se o material tiver o nome da textura)
        for (int i = 0; i < guids.Length; i++)
        {
            string texPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            string texName = Path.GetFileNameWithoutExtension(texPath);

            // Ignora a pasta Normals / mapas de relevo
            if (texPath.ToLower().Contains("normals") || texPath.ToLower().Contains("bump"))
                continue;

            if (mat.name.ToLower().Contains(texName.ToLower()) || texName.ToLower().Contains(mat.name.ToLower()))
            {
                return (Texture)AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D));
            }
        }

        // Prioridade B: Pega a textura de cor principal da pasta Textures do pacote (Atlas)
        for (int i = 0; i < guids.Length; i++)
        {
            string texPath = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (texPath.ToLower().Contains("normals") || texPath.ToLower().Contains("bump"))
                continue;

            return (Texture)AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D));
        }

        return null;
    }
}