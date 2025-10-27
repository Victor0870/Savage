using System;
using System.Collections.Generic;

namespace CBS.Models
{
    // Class này cần kế thừa từ FunctionBaseRequest hoặc tương đương
    public class FunctionEndGameExpRequest : FunctionBaseRequest
    {
        // Đây là tham số duy nhất bạn gửi lên Azure Function
        public bool IsCompleted { get; set; }
    }
}