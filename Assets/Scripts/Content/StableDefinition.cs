using UnityEngine;

namespace TaskbarTactics.Content
{
    public abstract class StableDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable identifier persisted in save files.")]
        private string id = string.Empty;

        public string Id => id;

        protected void SetId(string value)
        {
            id = value;
        }
    }
}
