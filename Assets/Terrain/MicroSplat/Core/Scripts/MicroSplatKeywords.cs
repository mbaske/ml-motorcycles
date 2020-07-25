using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicroSplatKeywords : ScriptableObject
{
	public List<string> keywords = new List<string> ();

	// geometry-100
	public int drawOrder = 100;

	public bool IsKeywordEnabled (string k)
	{
		return (keywords.Contains (k));
	}

	public void EnableKeyword (string k)
	{
		if (!IsKeywordEnabled (k)) {
			keywords.Add (k);
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty (this);
#endif
		}

	}

	public void DisableKeyword (string k)
	{
		if (IsKeywordEnabled (k)) {
			keywords.Remove (k);
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty (this);
#endif
		}
	}
}
