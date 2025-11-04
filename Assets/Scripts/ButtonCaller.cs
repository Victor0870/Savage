using UnityEngine;

public class ButtonCaller : MonoBehaviour
{
    // Phương thức này sẽ được gán vào sự kiện On Click() của nút trên Prefab
    public void CallMoveObject()
    {
        // Kiểm tra xem Singleton có tồn tại không
        if (MoveObject.Instance != null)
        {
            // Gọi hàm di chuyển thông qua Instance tĩnh của Singleton
            MoveObject.Instance.MoveUpOnClick();
        }
        else
        {
            Debug.LogError("Lỗi: Không tìm thấy đối tượng MoveObject trên Scene!");
        }
    }
    public void CallMoveObjectDown()
    {
        if (MoveObject.Instance != null)
        {
            MoveObject.Instance.MoveDownOnClick(); // Gọi hàm đi xuống
        }
        else
        {
            Debug.LogError("Lỗi: Không tìm thấy đối tượng MoveObject trên Scene!");
        }
    }
}