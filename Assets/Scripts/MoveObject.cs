using UnityEngine;

public class MoveObject : MonoBehaviour
{
    // Tham chiếu Singleton (công khai và tĩnh)
    public static MoveObject Instance { get; private set; }

    public float moveDistance = 5f;

    // Khởi tạo Singleton
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // HÀM ĐƯỢC GỌI TỪ BÊN NGOÀI
    public void MoveUpOnClick()
    {
        // Thực hiện lệnh teleport ngay lập tức
        transform.position += new Vector3(0, moveDistance, 0);
        Debug.Log("Object đã di chuyển thông qua Singleton.");
    }

    public void MoveDownOnClick()
        {
            // Trừ moveDistance theo trục Y (dùng -moveDistance)
            transform.position += new Vector3(0, -moveDistance, 0);
            Debug.Log("Object đã di chuyển XUỐNG thông qua Singleton.");
        }
}