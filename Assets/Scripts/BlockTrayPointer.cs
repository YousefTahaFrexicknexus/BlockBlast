using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BlockTrayPointer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private BlockBlastPrototype game;
    private int slot;

    public void Initialize(BlockBlastPrototype prototype, int index)
    {
        game = prototype;
        slot = index;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        game.StartPieceDrag(slot, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        game.ContinuePieceDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        game.FinishPieceDrag(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        game.SelectPiece(slot);
    }
}
