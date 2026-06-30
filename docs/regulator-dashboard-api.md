# Dashboard API Handoff

This guide is for the teams building the SIPS Regulator Dashboard and SIPS Board Dashboard.

Dashboard applications only need to:

1. Log in.
2. Store the returned tokens.
3. Fetch dashboard endpoints with `Authorization: Bearer <accessToken>`.
4. Refresh the token when needed.

Dashboard applications should not call ingestion endpoints.

## 1. Authentication Flow

### Login

```http
POST /api/v1/auth/login
Content-Type: application/json
```

Request:

```json
{
  "username": "admin",
  "password": "password",
  "otp": null
}
```

Response:

```json
{
  "data": {
    "accessToken": "<jwt-access-token>",
    "refreshToken": "REDACTED_SECRET",
    "expiresIn": 1200,
    "refreshExpiresIn": 21600
  },
  "isSuccess": true,
  "statusCode": 200,
  "message": ""
}
```

Use `data.accessToken` for all protected dashboard requests.

### Fetch With Token

```http
GET /api/regulator-dashboard/realtime-monitoring-cards?period=today
Authorization: Bearer <accessToken>
```

### Refresh Token

```http
POST /api/v1/auth/refresh
Content-Type: application/json
```

Request:

```json
{
  "accessToken": "<current-or-expired-access-token>",
  "refreshToken": "REDACTED_SECRET"
}
```

Response:

```json
{
  "data": {
    "accessToken": "<new-jwt-access-token>",
    "refreshToken": "REDACTED_SECRET",
    "expiresIn": 1200,
    "refreshExpiresIn": 21600
  },
  "isSuccess": true,
  "statusCode": 200,
  "message": ""
}
```

Refresh tokens are single-use. After refresh succeeds, replace both saved tokens.

## 2. Token Saving Design

Recommended client-side token state:

```ts
type AuthSession = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: number;
  refreshTokenExpiresAt: number;
};
```

When login succeeds:

```ts
const now = Date.now();

const session = {
  accessToken: response.data.accessToken,
  refreshToken: response.data.refreshToken,
  accessTokenExpiresAt: now + response.data.expiresIn * 1000,
  refreshTokenExpiresAt: now + response.data.refreshExpiresIn * 1000
};
```

Storage recommendation:

- Keep `accessToken` in memory when possible.
- Store `refreshToken` in the most secure storage available to the app.
- If browser storage is required, prefer `sessionStorage` over `localStorage` unless product requirements need persistent login.
- Do not store user passwords.
- Do not store API client secrets in the dashboard app.

Suggested fetch behavior:

```ts
async function dashboardFetch(path: string) {
  if (Date.now() > session.accessTokenExpiresAt - 60_000) {
    await refreshSession();
  }

  const response = await fetch(path, {
    headers: {
      Authorization: `Bearer ${session.accessToken}`
    }
  });

  if (response.status === 401) {
    await refreshSession();
    return fetch(path, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`
      }
    });
  }

  return response;
}
```

On refresh failure:

1. Clear saved tokens.
2. Send the user back to login.

## 3. Permissions

Dashboard endpoints generally require:

```text
QFT
```

Existing rich financial reports require:

```text
QRPT
```

Error meaning:

- `401`: user is not logged in, token is missing, or token is expired/invalid.
- `403`: user is logged in but does not have the required permission.

## 4. Date Filters

Most dashboard endpoints support `period`.

```http
?period=today
?period=last-hour
?period=last-24-hours
?period=yesterday
?period=week-to-date
?period=month-to-date
?period=last-week
?period=last-month
?period=last-quarter
?period=last-year
```

Most dashboard endpoints also support custom ranges:

```http
?from=2026-06-01T00:00:00Z&to=2026-06-06T23:59:59Z
```

If no date filter is supplied, the API defaults to:

```http
?period=today
```

## 5. Regulator Dashboard Endpoints

### Realtime Monitoring Cards

```http
GET /api/regulator-dashboard/realtime-monitoring-cards?period=today
```

Use for top KPI cards.

```json
{
  "data": [
    {
      "id": 1,
      "title": "Failed Transaction Rate",
      "value": "3.2 bps",
      "description": "Failed attempts vs successful non-fee settlements"
    },
    {
      "id": 2,
      "title": "Active Velocity Alerts",
      "value": "14",
      "description": "3 escalated to FIU"
    },
    {
      "id": 3,
      "title": "Successful Transactions",
      "value": "102,340",
      "description": "Non-fee settlement transactions in the selected window"
    },
    {
      "id": 4,
      "title": "Successful Settlement Value",
      "value": "511,000,000.00",
      "description": "Total value excluding fee transactions"
    },
    {
      "id": 5,
      "title": "Endpoint Availability",
      "value": "99.4%",
      "description": "1,240 successful of 1,248 checks"
    },
    {
      "id": 6,
      "title": "Offline Participants",
      "value": "2",
      "description": "2 with active incidents"
    }
  ]
}
```

### Transaction Volume Hourly

```http
GET /api/regulator-dashboard/transaction-volume-hourly?period=today
```

Volume means transaction count.

```json
{
  "data": {
    "00:00": 12450,
    "01:00": 8970,
    "02:00": 0,
    "14:00": 102340,
    "23:00": 18760
  }
}
```

### Velocity Alerts

```http
GET /api/regulator-dashboard/velocity-alerts?period=last-week&limit=50
```

```json
{
  "data": [
    {
      "id": "1",
      "alertId": "VA-2026-0042-01",
      "institution": "Daryeel Bank",
      "pattern": "Burst P2P",
      "txCount": 68,
      "timeWindow": "14:00 - 15:00",
      "value": 4240,
      "severity": "HIGH",
      "status": "Escalated to FIU",
      "timestamp": "2026-06-06 14:28:32"
    }
  ]
}
```

Pattern mapping is provisional and should be validated by the business/risk team.

### Velocity Alert Heatmap

```http
GET /api/regulator-dashboard/velocity-alert-heatmap?period=last-month
```

```json
{
  "data": {
    "maxAlerts": 156,
    "institutions": [
      {
        "name": "Premier Bank",
        "patterns": {
          "burstP2P": 31,
          "smurfing": 45,
          "roundTrip": 18,
          "velocity": 29,
          "structuring": 22,
          "burstP2M": 11
        },
        "totalAlerts": 156,
        "riskScore": 88,
        "peakTime": "13:00 - 14:00"
      }
    ]
  }
}
```

### Participant Availability

```http
GET /api/regulator-dashboard/participant-availability?period=today
```

```json
{
  "data": {
    "summary": {
      "totalParticipants": 12,
      "liveParticipants": 10,
      "offlineParticipants": 2,
      "openIncidents": 2,
      "totalChecks": 1440,
      "successChecks": 1420,
      "failedChecks": 20,
      "uptimePercent": 98.61
    },
    "participants": [
      {
        "institutionBic": "SALSOSMXXX",
        "institutionName": "Salaam Somali Bank",
        "isLive": true,
        "lastCheckedAt": "2026-06-06T09:30:00Z",
        "lastStatusChangeAt": "2026-06-06T08:00:00Z",
        "consecutiveFailures": 0,
        "consecutiveSuccesses": 42,
        "lastError": null,
        "totalChecks": 120,
        "successChecks": 120,
        "failedChecks": 0,
        "uptimePercent": 100,
        "lastFailureAt": null
      }
    ]
  }
}
```

## 6. Board Dashboard Endpoints

### Cards

```http
GET /api/dashboard/cards?period=today
```

```json
{
  "data": [
    {
      "id": 1,
      "title": "System Availability",
      "value": "99.96%",
      "description": "Uptime across switch, endpoints & components"
    },
    {
      "id": 2,
      "title": "Systemic Risk Score",
      "value": "LOW",
      "description": "Liquidity + Fraud + AML"
    },
    {
      "id": 3,
      "title": "Fee per Transaction",
      "value": "$0.00",
      "description": "No transaction fees currently applied"
    }
  ]
}
```

### Total System Value

```http
GET /api/dashboard/total-system-value?months=8
```

```json
{
  "data": [
    {
      "month": "Jan 2025",
      "value": 425000000
    },
    {
      "month": "Feb 2025",
      "value": 438000000
    }
  ]
}
```

### Transaction Type Mix

```http
GET /api/dashboard/transaction-type-mix?period=today
```

```json
{
  "data": {
    "p2p": 18420000,
    "p2m": 6310000,
    "g2p": 1840000,
    "b2b": 890000
  }
}
```

The endpoint always returns `p2p`, `p2m`, `g2p`, and `b2b`. Counts depend on `CategoryPurpose` values.

### Financial Inclusion

```http
GET /api/dashboard/financial-inclusion
```

This endpoint is fetchable now. It returns default values until the business source data is available.

```json
{
  "nationalData": {
    "name": "NATIONAL TOTAL",
    "adultPopulation": 0,
    "activeUsers": 0,
    "coverage": 0,
    "targetGap": 0
  }
}
```

### New Accounts Onboarding

```http
GET /api/dashboard/new-accounts-onboarding
```

This endpoint is fetchable now. It returns empty data until the business source data is available.

```json
{
  "monthlyData": {}
}
```

### Rulebook Compliance Score

```http
GET /api/dashboard/rulebook-compliance-score
```

This endpoint is fetchable now. It returns default values until the business source data is available.

```json
{
  "score": 0,
  "previousScore": 0,
  "totalReviews": 0,
  "passedReviews": 0,
  "failedReviews": 0,
  "lastReviewDate": null
}
```

## 7. Full Transaction Detail Fetch Endpoints

Use these endpoints when a dashboard screen needs transaction-row drilldown instead of aggregate cards or charts.

These endpoints:

- Require `Authorization: Bearer <accessToken>`.
- Require the `QFT` permission.
- Return paginated responses.
- Support the same common filters.

Common query parameters:

```http
Index=1
Size=100
BankFrom=SALSOSMXXX
BankTo=DARYSOSMXXX
CreatedDate=2026-06-06
From=2026-06-06T09:00:00Z
To=2026-06-06T10:00:00Z
TxId=TX-123
Type=FP_PAYMENT
Status=DECLINED
SttlId=10001
DbtrAcctId=25261...
CdtrAcctId=25263...
LocalInstrument=CRTRM
CategoryPurpose=C2CCRT
TxSts=RJCT
Error=WBIC
FailureReason=Black List
```

Use `CreatedDate` for a single calendar day. Use `From` and `To` for dashboard ranges such as last hour, today, last week, or last month.

### Unified Transactions

```http
GET /api/v1/Transactions?Index=1&Size=100&Type=50&Status=2&From=2026-06-20T00:00:00Z&To=2026-06-21T00:00:00Z
```

This is the recommended transaction drilldown endpoint. By default, it returns the new Oracle bridge rows saved from `POST /api/v1/balance/transactions`.

Optional `Kind` values:

- `oracle`: default; returns rows from `oracletransactions`.
- `all`: returns Oracle rows plus legacy failed/processed rows.
- `legacy`: returns only legacy failed and processed rows.
- `failed`: returns only legacy failed rows.
- `processed`: returns only legacy processed rows.

Oracle response rows include the normalized fields plus Oracle-specific fields such as `typeDescription`, `amountValue`, `txSts`, `lclInstrm`, `ctgyPurp`, `endToEndId`, `attributesRaw`, and `rawPayload`.

Consumer-friendly aliases are also returned:

- `localInstrument`: same source as Oracle `lclInstrm`.
- `categoryPurpose`: same source as Oracle `ctgyPurp`.
- `failureCode`: same source as Oracle `error`.
- `failureDescription`: same source as Oracle `errDesc`.
- `failureReason`: best available reason from `errorReason`, `stsRsnInfAddtlInf`, `errDesc`, or `error`.

Example failure drilldown:

```http
GET /api/v1/Transactions?TxSts=RJCT&LocalInstrument=CRTRM&CategoryPurpose=C2CCRT&FailureReason=Black%20List
```

### Legacy Failed Transactions

```http
GET /api/v1/FailedTransactions?Index=1&Size=100&Type=FP_PAYMENT&Status=DECLINED&From=2026-06-06T09:00:00Z&To=2026-06-06T10:00:00Z
```

Response shape:

```json
{
  "data": [
    {
      "id": 9911,
      "bankFrom": "SALSOSMXXX",
      "bankFromName": "Salaam Somali Bank",
      "bankTo": "DARYSOSMXXX",
      "bankToName": "Daryeel Bank",
      "createdDate": "2026-06-06T09:15:22Z",
      "modifiedDate": "2026-06-06T09:15:25Z",
      "acceptanceDate": "2026-06-06T09:15:22Z",
      "txId": "TX-20260606-0001",
      "type": "FP_PAYMENT",
      "amount": "125.50",
      "status": "DECLINED",
      "description": "Payment rejected",
      "sttlId": 10001,
      "currency": "SOS",
      "intrBkSttlmAtm": "125.50",
      "dbtrAcctId": "25261...",
      "dbtrAcctType": "MOBILE",
      "dbtrName": "Customer A",
      "dbtrAddress": null,
      "cdtrAcctId": "25263...",
      "cdtrName": "Customer B",
      "cdtrAddress": null,
      "requestId": "REQ-001",
      "transCode": "FP",
      "error": "DECLINED",
      "errorReason": "Insufficient funds"
    }
  ],
  "index": 1,
  "size": 100,
  "count": 1,
  "hasPrevious": false,
  "hasNext": false
}
```

### Legacy Processed Transactions

```http
GET /api/v1/ProcessedTransactions?Index=1&Size=100&Type=FP_PAYMENT&Status=PROCESSED&From=2026-06-06T09:00:00Z&To=2026-06-06T10:00:00Z
```

Response shape:

```json
{
  "data": [
    {
      "id": 120044,
      "bankFrom": "SALSOSMXXX",
      "bankFromName": "Salaam Somali Bank",
      "bankTo": "DARYSOSMXXX",
      "bankToName": "Daryeel Bank",
      "createdDate": "2026-06-06T09:20:00Z",
      "modifiedDate": "2026-06-06T09:20:04Z",
      "acceptanceDate": "2026-06-06T09:20:00Z",
      "txId": "TX-20260606-0002",
      "type": "FP_PAYMENT",
      "amount": "340.00",
      "status": "PROCESSED",
      "description": "Payment processed",
      "sttlId": 10002,
      "currency": "SOS",
      "intrBkSttlmAtm": "340.00",
      "dbtrAcctId": "25261...",
      "dbtrAcctType": "MOBILE",
      "dbtrName": "Customer A",
      "dbtrAddress": null,
      "cdtrAcctId": "25263...",
      "cdtrName": "Customer B",
      "cdtrAddress": null,
      "requestId": "REQ-002",
      "transCode": "FP",
      "receivedAt": "2026-06-06T09:21:10Z"
    }
  ],
  "index": 1,
  "size": 100,
  "count": 1,
  "hasPrevious": false,
  "hasNext": false
}
```

## 8. Optional Existing Rich Report

This endpoint is not required for the dashboard widgets above, but it is available for deeper reporting screens.

```http
GET /api/v1/reports/FinancialReports?fromDate=2026-06-01T00:00:00Z&toDate=2026-06-06T23:59:59Z
```

Requires:

```text
QRPT
```

## 9. Data Interpretation Notes

- Successful transactions are read from `NetSettlementDetails`.
- Fee transactions are excluded from dashboards for now.
- Hourly transaction volume is count, not value.
- Participant availability comes from monitoring data.
- New Oracle bridge transaction rows are saved from `POST /api/v1/balance/transactions`.
- Legacy failed transaction rows are saved from `POST /api/v1/balance/failed-transactions`.
- Legacy processed transaction rows are saved from `POST /api/v1/balance/processed-transactions`.
- Summary-only transaction webhook totals are saved and can support failed-rate calculations when summary mode is enabled.
- Some board endpoints are already fetchable but intentionally return defaults until business source data is provided.

## 10. Service-Only Endpoints

These endpoints are not for dashboard clients.

### Monitoring Availability Ingestion

```http
POST /api/v1/history/monit
```

Used by the monitoring service to send participant availability checks.

### Oracle Bridge Full Transaction Receiver

```http
POST /api/v1/balance/transactions
Content-Type: application/json
X-API-Key: <ApiAuth clientId>
X-API-Secret: <ApiAuth secret>
```

This is the current receiver endpoint for full transaction batches from the Oracle transaction bridge.

Durable persistence:

- Batch receipts are saved in `oracletransactioningestionbatches`.
- Transaction rows are saved in `oracletransactions`.
- The batch idempotency key is `source + from_id + to_id + high_watermark_id`.
- The row idempotency key is `source + id`.

Success responses:

- `202 Accepted` for a newly stored batch.
- `200 OK` for a duplicate batch that was already stored.

Validation failures return `400 Bad Request`, which tells the producer not to advance its checkpoint.

Receiver config:

```json
{
  "transactions_webhook_url": "https://<spss-api-host>/api/v1/balance/transactions",
  "webhook_api_key": "<ApiAuth clientId>",
  "webhook_secret": "<ApiAuth secret>"
}
```

Example success body:

```json
{
  "data": {
    "status": "accepted",
    "batch_key": "oracle:20700:20716:20716",
    "received": 2,
    "duplicates": 0,
    "inserted": 2,
    "updated": 0
  },
  "isSuccess": true,
  "statusCode": 202,
  "message": ""
}
```

### Legacy Balance Monitoring Full-Detail Configuration

Configure the balance-monitoring service to send full failed and processed rows:

```json
{
  "failed_transactions_enabled": true,
  "processed_transactions_enabled": true,
  "failed_transactions_webhook_url": "https://<spss-api-host>/api/v1/balance/failed-transactions",
  "failed_transactions_webhook_method": "POST",
  "processed_transactions_webhook_url": "https://<spss-api-host>/api/v1/balance/processed-transactions",
  "processed_transactions_webhook_method": "POST",
  "processed_transactions_start_hour": 2,
  "processed_transactions_end_hour": 2,
  "webhook_api_key": "<ApiAuth clientId>",
  "webhook_secret": "<ApiAuth secret>"
}
```

For processed transaction polling, setting `processed_transactions_start_hour` and `processed_transactions_end_hour` to the same valid hour keeps the processed worker eligible all day.

If the service should send full rows only, leave `transaction_summary_webhook_url` empty or unset. If the backend also needs aggregate checkpoints, set it to:

```json
{
  "transaction_summary_webhook_url": "https://<spss-api-host>/api/v1/balance/transaction-summary",
  "transaction_summary_webhook_method": "POST"
}
```

### Legacy Failed Transaction Detail Ingestion

```http
POST /api/v1/balance/failed-transactions
```

Used by balance monitoring to send full failed transaction rows. The rows are saved in the database and can be fetched through `GET /api/v1/FailedTransactions`.

### Legacy Processed Transaction Detail Ingestion

```http
POST /api/v1/balance/processed-transactions
```

Used by balance monitoring to send full processed transaction rows. The rows are saved in the database and can be fetched through `GET /api/v1/ProcessedTransactions`.

### Transaction Summary Ingestion

```http
POST /api/v1/balance/transaction-summary
```

Used by balance monitoring to send summary-only failed/processed totals. Use this only when the service is not sending full transaction details, or when the backend also needs an aggregate checkpoint.

```json
{
  "timestamp": "2026-06-06T10:00:00Z",
  "total_failed": 12,
  "total_processed": 340,
  "failed_from_date": "2026-06-06T09:00:00Z",
  "failed_to_date": "2026-06-06T10:00:00Z",
  "processed_from_date": "2026-06-06T09:00:00Z",
  "processed_to_date": "2026-06-06T10:00:00Z",
  "failed_status": "DECLINED",
  "processed_status": "PROCESSED",
  "transaction_type": "FP_PAYMENT"
}
```
