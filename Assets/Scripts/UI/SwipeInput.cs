using UnityEngine;
using UnityEngine.EventSystems;

namespace TiltMirror
{
    // Put this on a full-screen UI Image (Raycast Target on, alpha can be 0).
    // Works with both the old and the new Input System UI modules because it only
    // uses EventSystem drag callbacks. Needs an EventSystem in the scene.
    public sealed class SwipeInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] GameController controller;
        [Tooltip("Minimum swipe length as a fraction of screen height.")]
        [SerializeField] float minSwipeFraction = 0.04f;

        Vector2 startPosition;

        public void OnBeginDrag(PointerEventData eventData)
        {
            startPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Required by IDragHandler so that drag events are delivered.
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - startPosition;
            if (delta.magnitude < Screen.height * minSwipeFraction) return;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                if (delta.x > 0f) controller.MoveRight(); else controller.MoveLeft();
            }
            else
            {
                if (delta.y > 0f) controller.MoveUp(); else controller.MoveDown();
            }
        }
    }
}
