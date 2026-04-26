using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZombieBunker
{
    public class ClearSave : MonoBehaviour
    {
        public void ClearAllSaveData()
        {
            // Disable and destroy existing SaveManager to prevent auto-save
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DisableSave();
                Destroy(SaveManager.Instance.gameObject);
            }

            // Clear PlayerPrefs
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Debug.Log("Save data cleared!");

            // Reload scene next frame to ensure SaveManager is gone
            StartCoroutine(ReloadSceneNextFrame());
        }

        private IEnumerator ReloadSceneNextFrame()
        {
            yield return null; // wait one frame
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}