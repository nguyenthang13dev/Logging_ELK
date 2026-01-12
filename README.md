# 📘 TÀI LIỆU HỆ THỐNG LOGGING

## 1. Mục tiêu

Hệ thống logging được xây dựng nhằm:

* Thu thập **logs / traces / metrics** tập trung từ các dịch vụ .NET Core API
* Chuẩn hoá theo **OpenTelemetry (OTLP)**
* Lưu trữ, tìm kiếm, phân tích log bằng **Elasticsearch**
* Trực quan hoá, giám sát qua **Kibana**

---

## 2. Kiến trúc tổng thể

```
.NET Core API 
   │

   Serilog (thread log riêng)

   │ OpenTelemetry SDK (OTLP)
   ▼
OpenTelemetry Collector
   │
   │ OTLP / HTTP
   ▼
Elasticsearch
   │
   ▼
Kibana
```

### Thành phần

| Thành phần        | Vai trò                         |
| ----------------- | ------------------------------- |
| .NET Core API     | Phát sinh log / trace / metric  |
| OTEL Collector    | Thu gom, xử lý, forward dữ liệu |
| Elasticsearch     | Lưu trữ & tìm kiếm              |
| Kibana            | Dashboard, truy vấn, cảnh báo   |

---


### 3.2 Level log

| Level       | Ý nghĩa            |
| ----------- | ------------------ |
| Trace       | Debug chi tiết     |
| Debug       | Phục vụ dev        |
| Information | Hành vi hệ thống   |
| Warning     | Bất thường nhẹ     |
| Error       | Lỗi nghiệp vụ      |
| Critical    | Sự cố nghiêm trọng |

## 5. OpenTelemetry Collector

### 5.1 Vai trò

* Nhận OTLP từ services
* Batching, filtering
* Forward sang Elasticsearch

### 5.2 Cấu hình `otel-collector.yaml`


## 6. Elasticsearch

### 6.1 Index strategy

* logs-system-YYYY.MM.DD
* Sử dụng **ILM (Index Lifecycle Management)**

### 6.2 ILM đề xuất

| Phase  | Thời gian | Hành động    |
| ------ | --------- | ------------ |
| Hot    | 0–7 ngày  | Ghi log      |
| Warm   | 7–30 ngày | Giảm replica |
| Delete | >30 ngày  | Xoá          |

---

## 7. Kibana

### 7.1 Chức năng chính

* Discover logs
* Dashboard theo service
* Trace view (distributed tracing)
* Alerting

### 7.2 Dashboard gợi ý

* Error rate theo service
* Request latency (p95)
* Top exception
* Log theo trace_id

---

## 8. Truy vết (Trace)

* Mỗi request sinh ra `trace_id`
* Log – Trace – Metric được liên kết
* Dễ debug lỗi phân tán (microservices)

---

## 11. Cron Job & Cơ chế xóa log định kỳ

### 11.1 Mục tiêu

* Tránh **tràn dung lượng ổ đĩa** do log tăng liên tục
* Giảm chi phí lưu trữ Elasticsearch
* Tuân thủ chính sách lưu trữ dữ liệu (data retention policy)

---

### 11.2 Chiến lược xóa log

Hệ thống áp dụng **kết hợp 2 cơ chế**:

1. **ILM (Index Lifecycle Management)** – cơ chế chuẩn của Elasticsearch
2. **Cron Job chủ động** – dùng cho các trường hợp đặc biệt

---

### 11.3 ILM – Cơ chế chính (Khuyến nghị)
Elasticsearch tự động xoá index theo vòng đời:

| Phase  | Thời gian   | Hành động               |
| ------ | ----------- | ----------------------- |
| Hot    | 0 – 7 ngày  | Ghi log                 |
| Warm   | 7 – 30 ngày | Giảm replica / readonly |
| Delete | > 30 ngày   | Xoá index               |

📌 Ưu điểm:

* Không cần cron
* An toàn, native
* Hiệu năng tốt

📌 Áp dụng cho:

* logs-system-YYYY.MM.DD
* audit-log-YYYY.MM.DD

---

### 11.4 Cron Job – Cơ chế bổ trợ

Cron Job được dùng khi:

* Cần xoá log theo **điều kiện nghiệp vụ**
* Xoá log debug / test
* Dọn log tạm, log ngoài ILM


### 11.6 Giám sát & an toàn

✅ Log lại kết quả cron job

✅ Alert khi dung lượng disk > 80%

✅ Không chạy cron xoá log giờ cao điểm

---

## 12. Mở rộng trong tương lai

* Thêm Metrics (CPU, RAM)
* Alert qua Slack / Email
* APM nâng cao
* Correlation log – business event

---
