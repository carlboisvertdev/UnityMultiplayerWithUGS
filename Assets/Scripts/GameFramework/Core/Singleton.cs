using UnityEngine;

namespace GameFramework.Core
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T _instance;
        // Start is called before the first frame update
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    T[] objs = FindObjectsOfType<T>();
                    if (objs.Length > 0)
                    {
                        T instance = objs[0];
                        _instance = instance;
                    }
                    else
                    {
                        GameObject go = new GameObject();
                        go.name = typeof(T).Name;
                        _instance = go.AddComponent<T>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }
    }   
}