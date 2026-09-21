using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    [RequireComponent(typeof(Image))]
    public sealed class ItemHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerMoveHandler, IPointerClickHandler, IBeginDragHandler
    {
        private ItemTooltipView tooltip;
        private GameContentCatalog catalog;
        private InventoryItem item;
        private string language;
        private Image icon;
        private Color restingColor;
        private bool highlighted;

        public void Configure(ItemTooltipView view, GameContentCatalog content, InventoryItem instance,
            string languageCode)
        {
            ClearHover();
            tooltip = view;
            catalog = content;
            item = instance;
            language = languageCode;
            icon = GetComponent<Image>();
        }

        public void OnPointerEnter(PointerEventData eventData) => Show(eventData);
        public void OnPointerClick(PointerEventData eventData) => Show(eventData);
        public void OnPointerExit(PointerEventData eventData) => ClearHover();
        public void OnBeginDrag(PointerEventData eventData) => ClearHover();
        public void OnPointerMove(PointerEventData eventData) =>
            tooltip?.Move(this, eventData.position, eventData.enterEventCamera);

        private void Show(PointerEventData eventData)
        {
            if (item == null || tooltip == null || eventData.dragging ||
                !string.IsNullOrEmpty(InventoryItemDragSource.ActiveItemInstanceId) ||
                EquipmentItemDragSource.HasActiveItem) return;
            if (!highlighted && icon != null)
            {
                restingColor = icon.color;
                icon.color = new Color(1f, 0.93f, 0.72f, restingColor.a);
                highlighted = true;
            }
            tooltip.Show(this, catalog, item, language, eventData.position, eventData.enterEventCamera);
        }

        private void ClearHover()
        {
            tooltip?.Hide(this);
            if (highlighted && icon != null) icon.color = restingColor;
            highlighted = false;
        }

        private void OnDisable() => ClearHover();
    }
}
