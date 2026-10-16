using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BlockBoardPointer : MonoBehaviour, IPointerClickHandler
{
    private BlockBlastPrototype game;

    public void Initialize(BlockBlastPrototype prototype)
    {
        game = prototype;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        game.TapBoard(eventData);
    }
}
