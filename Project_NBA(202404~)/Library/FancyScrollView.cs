void IEndDragHandler.OnEndDrag(PointerEventData eventData)
// 스크롤이 매우 빠르게 발생하였으며, OnDrag()에서 이벤트를 처리하지 못했을 경우
// needAfterDragging : OnBeginDrag()에서의 위치와 OnDrag()에서의 위치가 동일하면 true
if (Time.time - dragBeginTime <= 1.0f && needAfterDragging)
{			
    needAfterDragging = false;

    // 드래그가 끝나는 시점에서의 현재 위치
    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
    viewport,
    eventData.position,
    eventData.pressEventCamera,
    out var dragPointerPosition))
    {
        var pointerDelta = dragPointerPosition - beginDragPointerPosition;

        var position = (scrollDirection == ScrollDirection.Horizontal ? -pointerDelta.x : pointerDelta.y)
                       / ViewportSize
                       * scrollSensitivity
                       + scrollStartPosition;

        var offset = CalculateOffset(position);
        position += offset;



        if (movementType == MovementType.Elastic)
        {
            if (offset != 0f)
            {
                position -= RubberDelta(offset, scrollSensitivity);
            }
        }

        UpdatePosition(position);

				// Update()에서 스크롤을 하기 위해 velocity를 만들어주는 용도	
        afterDragging = true;
    }
}