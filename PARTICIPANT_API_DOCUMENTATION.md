# Participant API Documentation

## Overview
This document provides comprehensive information for participants to consume the live participants and balance status endpoints. These endpoints allow participants to monitor the network status and their own balance information.

---

## Authentication

All endpoints require authentication using a JWT Bearer token.

### Headers
```http
Authorization: Bearer <your_jwt_token>
Content-Type: application/json
```

### Required Permission
- Role: `query_participants`

---

## Endpoints

### 1. Get Live Participants

Retrieve the live status of all participants in the payment switch network.

#### Endpoint
```http
GET /api/v1/participants/live
```

#### Query Parameters

| Parameter | Type    | Required | Description                                    |
|-----------|---------|----------|------------------------------------------------|
| `isLive`  | boolean | No       | Filter by live status. Omit to get all participants. |

#### Request Examples

**Get all participants:**
```http
GET /api/v1/participants/live
```

**Get only live participants:**
```http
GET /api/v1/participants/live?isLive=true
```

**Get only offline participants:**
```http
GET /api/v1/participants/live?isLive=false
```

#### Response

**Status Code:** `200 OK`

**Response Body:**
```json
{
  "data": [
    {
      "institutionBic": "AGROSOS0",
      "institutionName": "AGRO Bank",
      "isLive": true,
      "lastCheckedAt": "2026-01-01T09:40:11.920903+00:00",
      "lastStatusChangeAt": "2026-01-01T08:15:30.123456+00:00",
      "consecutiveFailures": 0,
      "consecutiveSuccesses": 145,
      "lastError": null,
      "currentBalance": 15000.50,
      "canSend": true,
      "thresholdZone": "Golden"
    },
    {
      "institutionBic": "ZKBASOS0",
      "institutionName": "Zirat Bank",
      "isLive": true,
      "lastCheckedAt": "2026-01-01T09:40:11.920958+00:00",
      "lastStatusChangeAt": "2026-01-01T07:22:15.987654+00:00",
      "consecutiveFailures": 0,
      "consecutiveSuccesses": 98,
      "lastError": null,
      "currentBalance": null,
      "canSend": true,
      "thresholdZone": "Amber"
    },
    {
      "institutionBic": "AALLSOS0",
      "institutionName": "Amal Bank",
      "isLive": false,
      "lastCheckedAt": "2026-01-01T09:40:11.920902+00:00",
      "lastStatusChangeAt": "2026-01-01T09:30:45.555555+00:00",
      "consecutiveFailures": 3,
      "consecutiveSuccesses": 0,
      "lastError": "Connection timeout",
      "currentBalance": null,
      "canSend": false,
      "thresholdZone": "Red"
    }
  ],
  "succeeded": true,
  "message": null,
  "errors": null
}
```

#### Response Fields

| Field                  | Type      | Description                                                                 |
|------------------------|-----------|-----------------------------------------------------------------------------|
| `institutionBic`       | string    | Bank Identifier Code (BIC) of the participant                              |
| `institutionName`      | string    | Name of the financial institution                                          |
| `isLive`               | boolean   | Whether the participant is currently live/online                           |
| `lastCheckedAt`        | datetime  | Timestamp of the last health check                                         |
| `lastStatusChangeAt`   | datetime  | Timestamp when the live status last changed                                |
| `consecutiveFailures`  | integer   | Number of consecutive failed health checks                                 |
| `consecutiveSuccesses` | integer   | Number of consecutive successful health checks                             |
| `lastError`            | string    | Error message from the last failed check (null if successful)              |
| `currentBalance`       | decimal   | Current balance (only visible for your own BIC, null for others)           |
| `canSend`              | boolean   | Whether the participant can send transactions                              |
| `thresholdZone`        | string    | Current balance zone: "Golden", "Amber", or "Red" (null if no threshold)   |

#### Important Notes

**Balance Privacy:**
- You can only see your own `currentBalance`
- Other participants' balances will always be `null` in the response
- This is determined by your authenticated BIC code (from JWT token's `tenantId` claim)

**Live Status:**
- `isLive: true` means the participant is reachable and responding to health checks
- `isLive: false` means the participant is currently unreachable or failing health checks

**Send Capability:**
- `canSend: true` means the participant can initiate outgoing transactions
- `canSend: false` means the participant is blocked from sending (usually due to insufficient balance)
- This does NOT affect the ability to receive transactions

**Threshold Zones:**
- **Golden**: Balance is above the minimum threshold (healthy)
- **Amber**: Balance is approaching the minimum threshold (warning)
- **Red**: Balance is below the minimum threshold (critical)
- **null**: No threshold configuration exists for this participant

---

### 2. Get Balance Status

Retrieve your own balance and threshold configuration details.

#### Endpoint
```http
GET /api/v1/participants/balance-status
```

#### Query Parameters
None

#### Request Example
```http
GET /api/v1/participants/balance-status
```

#### Response

**Status Code:** `200 OK`

**Response Body:**
```json
{
  "data": {
    "id": 1,
    "bic": "AGROSOS0",
    "goldenAmount": 10000.00,
    "amberPercentage": 20.00,
    "redPercentage": 10.00,
    "lastKnownZone": "Golden",
    "lastKnownBalance": 15000.50,
    "lastZoneChangeAt": "2026-01-01T08:15:30.123456+00:00",
    "lastAlertSentAt": null,
    "alertRecipients": "admin@agrobank.com,ops@agrobank.com",
    "enforceSendRestriction": true,
    "minimumSendBalance": 1000.00,
    "canSend": true
  },
  "succeeded": true,
  "message": null,
  "errors": null
}
```

#### Response Fields

| Field                    | Type     | Description                                                              |
|--------------------------|----------|--------------------------------------------------------------------------|
| `id`                     | integer  | Unique identifier for the threshold configuration                        |
| `bic`                    | string   | Your Bank Identifier Code                                                |
| `goldenAmount`           | decimal  | Minimum balance threshold (target balance)                               |
| `amberPercentage`        | decimal  | Warning threshold percentage (e.g., 20% means alert at 2000 if golden is 10000) |
| `redPercentage`          | decimal  | Critical threshold percentage (e.g., 10% means critical at 1000)         |
| `lastKnownZone`          | string   | Current balance zone: "Golden", "Amber", or "Red"                        |
| `lastKnownBalance`       | decimal  | Your current settlement account balance                                  |
| `lastZoneChangeAt`       | datetime | When the zone last changed                                               |
| `lastAlertSentAt`        | datetime | When the last alert email was sent (null if never sent)                  |
| `alertRecipients`        | string   | Comma-separated list of email addresses for alerts                       |
| `enforceSendRestriction` | boolean  | Whether send restrictions are enforced based on balance                  |
| `minimumSendBalance`     | decimal  | Minimum balance required to send transactions (if enforcement enabled)   |
| `canSend`                | boolean  | Whether you can currently send transactions                              |

#### Threshold Zone Calculation

The system calculates your current zone based on your balance and configured thresholds:

- **Golden Zone**: `balance >= goldenAmount`
- **Amber Zone**: `balance >= (goldenAmount * redPercentage / 100)` AND `balance < goldenAmount`
- **Red Zone**: `balance < (goldenAmount * redPercentage / 100)`

**Example:**
- `goldenAmount`: 10,000
- `amberPercentage`: 20% (2,000)
- `redPercentage`: 10% (1,000)

Zones:
- **Golden**: Balance ≥ 10,000
- **Amber**: 1,000 ≤ Balance < 10,000
- **Red**: Balance < 1,000

#### Send Restriction Logic

If `enforceSendRestriction` is `true`:
- `canSend = (currentBalance >= minimumSendBalance)`

If `enforceSendRestriction` is `false`:
- `canSend = true` (always allowed)

**Example:**
- `minimumSendBalance`: 1,000
- `currentBalance`: 800
- `enforceSendRestriction`: true
- **Result**: `canSend = false` (blocked from sending until balance reaches 1,000)

#### Error Responses

**404 Not Found** - No threshold configuration exists for your BIC:
```json
{
  "data": null,
  "succeeded": false,
  "message": "Threshold configuration not found",
  "errors": null
}
```

---

## Use Cases

### 1. Network Monitoring Dashboard

Query the live participants endpoint to display the current status of all participants in your dashboard:

```javascript
// Fetch all participants
const response = await fetch('/api/v1/participants/live', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});

const { data } = await response.json();

// Display live participants
const liveParticipants = data.filter(p => p.isLive);
console.log(`${liveParticipants.length} participants are currently live`);

// Display offline participants
const offlineParticipants = data.filter(p => !p.isLive);
console.log(`${offlineParticipants.length} participants are currently offline`);
```

### 2. Balance Monitoring

Check your own balance and send capability:

```javascript
// Fetch your balance status
const response = await fetch('/api/v1/participants/balance-status', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});

const { data } = await response.json();

if (!data.canSend) {
  console.warn('Warning: You cannot send transactions. Please top up your balance.');
  console.log(`Current balance: ${data.lastKnownBalance}`);
  console.log(`Minimum required: ${data.minimumSendBalance}`);
}

if (data.lastKnownZone === 'Red') {
  console.error('Critical: Your balance is in the RED zone!');
} else if (data.lastKnownZone === 'Amber') {
  console.warn('Warning: Your balance is in the AMBER zone.');
}
```

### 3. Pre-Transaction Validation

Before initiating a transaction, verify you can send:

```javascript
// Check if you can send before initiating a transaction
const liveResponse = await fetch('/api/v1/participants/live', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});

const { data: participants } = await liveResponse.json();
const myStatus = participants.find(p => p.institutionBic === myBic);

if (!myStatus.canSend) {
  throw new Error('Cannot send transactions: Insufficient balance');
}

if (!myStatus.isLive) {
  throw new Error('Cannot send transactions: System is offline');
}

// Proceed with transaction...
```

### 4. Recipient Validation

Check if the recipient bank is live before sending:

```javascript
async function canSendToRecipient(recipientBic) {
  const response = await fetch('/api/v1/participants/live', {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });

  const { data: participants } = await response.json();
  const recipient = participants.find(p => p.institutionBic === recipientBic);

  if (!recipient) {
    return { canSend: false, reason: 'Recipient not found in network' };
  }

  if (!recipient.isLive) {
    return { canSend: false, reason: 'Recipient is currently offline' };
  }

  return { canSend: true, reason: null };
}

// Usage
const result = await canSendToRecipient('ZKBASOS0');
if (!result.canSend) {
  console.error(`Cannot send: ${result.reason}`);
}
```

---

## Best Practices

### 1. Polling Frequency
- **Recommended**: Poll every 30-60 seconds for live status updates
- **Avoid**: Polling more frequently than every 10 seconds to prevent excessive load

### 2. Error Handling
Always handle potential errors gracefully:

```javascript
try {
  const response = await fetch('/api/v1/participants/live', {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }

  const result = await response.json();
  
  if (!result.succeeded) {
    throw new Error(result.message || 'Request failed');
  }

  // Process data
  const participants = result.data;
  
} catch (error) {
  console.error('Failed to fetch live participants:', error);
  // Show user-friendly error message
}
```

### 3. Caching
Consider caching the response for a short period to reduce API calls:

```javascript
let cachedData = null;
let cacheTime = null;
const CACHE_DURATION = 30000; // 30 seconds

async function getLiveParticipants() {
  const now = Date.now();
  
  if (cachedData && cacheTime && (now - cacheTime) < CACHE_DURATION) {
    return cachedData;
  }

  const response = await fetch('/api/v1/participants/live', {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });

  const result = await response.json();
  
  cachedData = result.data;
  cacheTime = now;
  
  return cachedData;
}
```

### 4. Token Refresh
Ensure your JWT token is valid and refresh it before expiration:

```javascript
async function fetchWithAuth(url) {
  // Check if token is about to expire
  if (isTokenExpiringSoon(token)) {
    token = await refreshToken();
  }

  return fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });
}
```

---

## Rate Limiting

To ensure fair usage and system stability:
- Maximum 120 requests per minute per participant
- Exceeding this limit will result in `429 Too Many Requests` responses
- Implement exponential backoff for retry logic

---

## Support

For technical support or questions about these endpoints:
- **Email**: support@paymentswitch.local
- **Documentation**: Check the main API documentation for additional endpoints
- **Status Page**: Monitor system-wide status at status.paymentswitch.local

---

## Changelog

### Version 1.0 (2026-01-01)
- Initial release of live participants endpoint
- Added balance status endpoint
- Implemented balance privacy (only own balance visible)
- Added send capability restrictions based on balance
- Added threshold zone monitoring
