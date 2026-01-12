# ?? TÀI LI?U H? TH?NG LOGGING

## 1. M?c tiêu

H? th?ng logging ???c xây d?ng nh?m:

* Thu th?p **logs / traces / metrics** t?p trung t? các d?ch v? .NET Core API
* Chu?n hoá theo **OpenTelemetry (OTLP)**
* L?u tr?, tìm ki?m, phân tích log b?ng **Elasticsearch**
* Tr?c quan hoá, giám sát qua **Kibana**

---

## 2. Ki?n trúc t?ng th?

```
.NET Core API 
   ?

   Serilog (thread log riêng)

   ? OpenTelemetry SDK (OTLP)
   ?
OpenTelemetry Collector
   ?
   ? OTLP / HTTP
   ?
Elasticsearch
   ?
   ?
Kibana
```

### Thành ph?n

| Thành ph?n        | Vai trò                         |
| ----------------- | ------------------------------- |
| .NET Core API     | Phát sinh log / trace / metric  |
| OTEL Collector    | Thu gom, x? lý, forward d? li?u |
| Elasticsearch     | L?u tr? & tìm ki?m              |
| Kibana            | Dashboard, truy v?n, c?nh báo   |

---


### 3.2 Level log

| Level       | Ý ngh?a            |
| ----------- | ------------------ |
| Trace       | Debug chi ti?t     |
| Debug       | Ph?c v? dev        |
| Information | Hành vi h? th?ng   |
| Warning     | B?t th??ng nh?     |
| Error       | L?i nghi?p v?      |
| Critical    | S? c? nghiêm tr?ng |

## 5. OpenTelemetry Collector

### 5.1 Vai trò

* Nh?n OTLP t? services
* Batching, filtering
* Forward sang Elasticsearch

### 5.2 C?u hình `otel-collector.yaml`


## 6. Elasticsearch

### 6.1 Index strategy

* logs-system-YYYY.MM.DD
* S? d?ng **ILM (Index Lifecycle Management)**

### 6.2 ILM ?? xu?t

| Phase  | Th?i gian | Hành ??ng    |
| ------ | --------- | ------------ |
| Hot    | 0–7 ngày  | Ghi log      |
| Warm   | 7–30 ngày | Gi?m replica |
| Delete | >30 ngày  | Xoá          |

---

## 7. Kibana

### 7.1 Ch?c n?ng chính

* Discover logs
* Dashboard theo service
* Trace view (distributed tracing)
* Alerting

### 7.2 Dashboard g?i ý

* Error rate theo service
* Request latency (p95)
* Top exception
* Log theo trace_id

---

## 8. Truy v?t (Trace)

* M?i request sinh ra `trace_id`
* Log – Trace – Metric ???c liên k?t
* D? debug l?i phân tán (microservices)

---

## 11. Cron Job & C? ch? xóa log ??nh k?

### 11.1 M?c tiêu

* Tránh **tràn dung l??ng ? ??a** do log t?ng liên t?c
* Gi?m chi phí l?u tr? Elasticsearch
* Tuân th? chính sách l?u tr? d? li?u (data retention policy)

---

### 11.2 Chi?n l??c xóa log

H? th?ng áp d?ng **k?t h?p 2 c? ch?**:

1. **ILM (Index Lifecycle Management)** – c? ch? chu?n c?a Elasticsearch
2. **Cron Job ch? ??ng** – dùng cho các tr??ng h?p ??c bi?t

---

### 11.3 ILM – C? ch? chính (Khuy?n ngh?)
Elasticsearch t? ??ng xoá index theo vòng ??i:

| Phase  | Th?i gian   | Hành ??ng               |
| ------ | ----------- | ----------------------- |
| Hot    | 0 – 7 ngày  | Ghi log                 |
| Warm   | 7 – 30 ngày | Gi?m replica / readonly |
| Delete | > 30 ngày   | Xoá index               |

?? ?u ?i?m:

* Không c?n cron
* An toàn, native
* Hi?u n?ng t?t

?? Áp d?ng cho:

* logs-system-YYYY.MM.DD
* audit-log-YYYY.MM.DD

---

### 11.4 Cron Job – C? ch? b? tr?

Cron Job ???c dùng khi:

* C?n xoá log theo **?i?u ki?n nghi?p v?**
* Xoá log debug / test
* D?n log t?m, log ngoài ILM


### 11.6 Giám sát & an toàn

? Log l?i k?t qu? cron job

? Alert khi dung l??ng disk > 80%

? Không ch?y cron xoá log gi? cao ?i?m

---

## 12. M? r?ng trong t??ng lai

* Thêm Metrics (CPU, RAM)
* Alert qua Slack / Email
* APM nâng cao
* Correlation log – business event

---
