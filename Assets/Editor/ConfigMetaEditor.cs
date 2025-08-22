using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.IO;



public class ConfigMetaEditor : EditorWindow
{
	/// <summary>
	/// 게임내 사용중인 데이터
	/// </summary>
	public SerializedObject instanceSO;

	/// <summary>
	/// 메타에서 불러온 데이터
	/// </summary>
	public SerializedObject metaSO;

	private ConfigMeta configMeta;
	private Vector2 scrollPos;
	private string filter;
	private GUIStyle labelStyle
	{
		get
		{
			GUIStyle style = new GUIStyle("Label");
			style.richText = true;

			return style;
		}
	}


	[MenuItem("Custom Menu/Config Meta")]
	public static void ShowEditor()
	{
		ConfigMetaEditor window = GetWindow<ConfigMetaEditor>();
		window.titleContent = new GUIContent(window.ToString());
		window.Show();
	}

	private void OnGUI()
	{

		// 초기화가 안된경우, 초기화 해줌
		if (instanceSO == null)
		{
            configMeta = Resources.Load<ConfigMeta>("RuntimeDatas/Config Meta");
            if (configMeta == null)
            {
                configMeta = ScriptableObject.CreateInstance<ConfigMeta>();
                AssetDatabase.CreateAsset(configMeta, "Assets/Resources/Config Meta.asset");
				AssetDatabase.SaveAssets();
                
                string path = AssetDatabase.GetAssetPath(configMeta);

				configMeta.path = path.Replace("Assets", Application.dataPath);
					 
                EditorUtility.SetDirty(this);
            }

            instanceSO = new SerializedObject(configMeta);
		}

        if (metaSO == null)
        {
            ReloadMetaConfig();
        }

        if (GUILayout.Button($"파일 위치({configMeta.path})"))
        {
            Application.OpenURL(configMeta.path);
        }
        if (GUILayout.Button($"파일 열기({ConfigMeta.fileName})"))
        {
            Application.OpenURL(configMeta.path + ConfigMeta.fileName);
        }

     

		// 버튼 메뉴 표시
		ShowButtonMenu();


		scrollPos = GUILayout.BeginScrollView(scrollPos);
		instanceSO.Update();
		metaSO.Update();
		// 데이터 리스트 표시
		ShowData();
		instanceSO.ApplyModifiedProperties();


		EditorGUILayout.Space(50);
		try
		{
			// Json으로 변환됬을때 표시
			ShowJsonData();
		}
		catch (Exception e)
		{
			GUILayout.Label("<color=red>Json Parsing Error(이게 뜨는경우 저장버튼 누르지마세요)</color>", labelStyle);
			GUILayout.Label($"<color=red>{e.Message}</color>", labelStyle);
		}
		GUILayout.EndScrollView();
	}
	string GetPath ()
	{
        string fileName = Path.GetFileName(configMeta.path);
        string path = configMeta.path.Replace(fileName, "");
		return path;
    }
	private void ShowButtonMenu()
	{
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Save"))
		{
			var json = JsonUtility.ToJson(instanceSO.targetObject, true);
			string path = GetPath();
            File.WriteAllText(path + ConfigMeta.fileName, json);
			AssetDatabase.Refresh();

			ReloadMetaConfig();
		}
		if (GUILayout.Button("Load"))
		{
            string path = GetPath();
            var json = File.ReadAllText(path + ConfigMeta.fileName);
			JsonUtility.FromJsonOverwrite(json, instanceSO.targetObject);

			ReloadMetaConfig();
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(10);
		filter = EditorGUILayout.TextField("Filter", filter);
	}

	private void ShowData()
	{
		GUILayout.Label("Data List", "PreToolbar");
		var fields = typeof(ConfigMeta).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		foreach (var field in fields)
		{
			foreach (var attribute in field.CustomAttributes)
			{
				if (attribute.AttributeType == typeof(TooltipAttribute))
				{
					string tooltipText = attribute.ConstructorArguments[0].Value.ToString();
					string propertyText = field.Name;
					if (string.IsNullOrEmpty(filter) == false)
					{
						if (tooltipText.ToLower().Contains(filter.ToLower()) || propertyText.ToLower().Contains(filter.ToLower()))
						{

						}
						else
						{
							continue;
						}
					}

					GUILayout.Label($"{tooltipText}", labelStyle);

					SerializedProperty prop = instanceSO.FindProperty(propertyText);

					float originWidth = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 300;


					GUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(prop);
					var instanceValue = field.GetValue(instanceSO.targetObject);
					var metaValue = field.GetValue(metaSO.targetObject);

					if (instanceValue.ToString() == metaValue.ToString())
					{
						GUILayout.Label($"  Meta: {metaValue}", labelStyle);
					}
					else
					{
						GUILayout.Label($"  <color=yellow>Meta: {metaValue}</color>", labelStyle);
					}
					GUILayout.EndHorizontal();


					EditorGUIUtility.labelWidth = originWidth;
					GUILayout.Space(10);
				}
			}
		}
	}

	private void ShowJsonData()
	{
		EditorGUILayout.LabelField("Json Data", new GUIStyle("PreToolbar"));
		EditorGUI.BeginDisabledGroup(true);
		EditorGUILayout.TextArea(JsonUtility.ToJson(instanceSO.targetObject, true));
		EditorGUI.EndDisabledGroup();
	}

	private void ReloadMetaConfig()
	{
		metaSO = new SerializedObject(CreateInstance<ConfigMeta>());

		if (File.Exists(configMeta.path + ConfigMeta.fileName))
		{
			var json = File.ReadAllText(configMeta.path + ConfigMeta.fileName);
			JsonUtility.FromJsonOverwrite(json, metaSO.targetObject);
		}
	}
}
