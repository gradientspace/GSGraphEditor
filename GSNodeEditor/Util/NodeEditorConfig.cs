// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Gradientspace.NodeGraph.ExecutionGraphSerializer;

namespace GSNodeEditor
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class GSConfigValue : Attribute
	{
		public GSConfigValue()
		{
		}
	}





	public static class NodeEditorConfig
	{
		static NodeEditorConfig()
		{
			LoadConfig();
		}

		public static string DefaultUserFilesPath {
			get {
				string UseRootPath = "";
				UseRootPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				if (Directory.Exists(UseRootPath) == false || UseRootPath.Length < 5 )		// try to detect this returning C:\ or / on linux
				{
					// should return homedir on *nix and C:\Users\username on Win
					UseRootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				}

				string GraphsPath = Path.Combine(UseRootPath, "nodegraphs");

				if (Directory.Exists(GraphsPath))
					return GraphsPath;

				try {
					DirectoryInfo newInfo = Directory.CreateDirectory(GraphsPath);
				}
				catch (Exception ex) {
					Debug.Assert(false);
				}

				if (Directory.Exists(GraphsPath) == false)
				{
					// what now??
					Debug.Assert(false);
				}

				return GraphsPath;
			}
		}


		public static string UserConfigFilePath {
			get {
				// todo on windows could use Environment.SpecialFolder.ApplicationData for AppData/Roaming folder...

				string HomeDirPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string GradientspacePath = Path.Combine(HomeDirPath, ".gradientspace");
				if (Directory.Exists(GradientspacePath) == false)
					Directory.CreateDirectory(GradientspacePath);

				string NodeEditorPath = Path.Combine(GradientspacePath, "GSNodeEditor");
				if (Directory.Exists(NodeEditorPath) == false)
					Directory.CreateDirectory(NodeEditorPath);

				string ConfigFilePath = Path.Combine(NodeEditorPath, "editor_config.json");
				return ConfigFilePath;
			}
		}


		[GSConfigValue]
		public static string LastFilePath { get; private set; } = "";


		public static void SetLastFilePath(string NewPath)
		{
			LastFilePath = NewPath;
			SaveConfig();
		}


		public static string GetActiveSaveLoadPath()
		{
			if (Directory.Exists(LastFilePath))
				return LastFilePath;
			return DefaultUserFilesPath;
		}



		[GSConfigValue]
		static List<string> RecentFiles = new List<string>();


		public static void AddToRecentFiles(string NewFile)
		{
			int Index = RecentFiles.IndexOf(NewFile);
			if ( Index >= 0 )
				RecentFiles.RemoveAt(Index);

			RecentFiles.Insert(0, NewFile);

			while (RecentFiles.Count > 20)
				RecentFiles.RemoveAt(RecentFiles.Count - 1);

			SaveConfig();
		}


		public static IEnumerable<string> EnumerateRecentFiles() {
			foreach (string filePath in RecentFiles)
			{
				if (File.Exists(filePath))
					yield return filePath;
			}
		}


		[GSConfigValue]
		public static bool LoadLastGraphOnStartup = false;



		// below actually implements very general automatic field/property serialization and
		// probably should be generalized outside this class...

		public static bool SaveConfig() 
		{
			Dictionary<string, object> ConfigValues = new Dictionary<string, object>();
			foreach (var Member in typeof(NodeEditorConfig).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ) {
				if ( Member.GetCustomAttribute<GSConfigValue>() != null ) {
					object? value = null;
					if (Member is PropertyInfo)
						value = ((PropertyInfo)Member).GetValue(null);
					else if ( Member is FieldInfo )
						value = ((FieldInfo)Member).GetValue(null);
					if (value != null)
						ConfigValues.Add(Member.Name, value);
				}
			}

			try {  
				using (MemoryStream memoryStream = new MemoryStream())
				{
					JsonSerializerOptions options = new JsonSerializerOptions();
					options.WriteIndented = true;
					JsonSerializer.Serialize<Dictionary<string, object>>(memoryStream, ConfigValues, options);
					memoryStream.Seek(0, SeekOrigin.Begin);
					File.Delete(UserConfigFilePath);
					using (FileStream fileStream = File.OpenWrite(UserConfigFilePath)) {
						memoryStream.CopyTo(fileStream);
						return true;
					}
				}
			} 
			catch (Exception)
			{
				return false;
			}

		}

		public static bool LoadConfig()
		{

			Dictionary<string, MemberInfo> ConfigProperties = new Dictionary<string, MemberInfo>();
			foreach (var Member in typeof(NodeEditorConfig).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ) {
				if ( Member.GetCustomAttribute<GSConfigValue>() != null ) {
					object? value = null;
					if (Member is PropertyInfo)
						value = ((PropertyInfo)Member).GetValue(null);
					else if ( Member is FieldInfo )
						value = ((FieldInfo)Member).GetValue(null);

					if (value != null)
						ConfigProperties.Add(Member.Name, Member);
				}
			}

			if (File.Exists(UserConfigFilePath) == false)
				return false;

			try {  
				using (FileStream fileStream = File.OpenRead(UserConfigFilePath))
				{
					Dictionary<string, object>? RestoredValues = JsonSerializer.Deserialize<Dictionary<string, object>>(fileStream);
					if (RestoredValues == null)
						return false;

					foreach ( KeyValuePair<string,object> ConfigVal in RestoredValues )
					{
						if ( ConfigProperties.TryGetValue(ConfigVal.Key, out MemberInfo? Member) )
						{
							if (Member == null || !(ConfigVal.Value is JsonElement))
								continue;

							JsonElement element = (JsonElement)ConfigVal.Value;

							if (Member is PropertyInfo)
							{
								PropertyInfo prop = ((PropertyInfo)Member);
								object? readObj = JsonSerializer.Deserialize(element, prop.PropertyType);
								prop.SetValue(null, readObj);
							}
							else if (Member is FieldInfo)
							{
								FieldInfo field = ((FieldInfo)Member);
								object? readObj = JsonSerializer.Deserialize(element, field.FieldType);
								((FieldInfo)Member).SetValue(null, readObj);
							}
						}
					}

					return true;
				}
			} 
			catch (Exception ex)
			{
				return false;
			}
		}


	}
}
