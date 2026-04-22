# MedBridge EMR — Blockchain Audit System

## Overview

MedBridge uses a **blockchain-inspired, tamper-evident audit ledger** to record every medication dispense, prescription, stock adjustment, procurement action, and user management event. The chain is stored in the `BlockchainBlocks` table and linked to the `AuditLogs` table for dual-record integrity.

---

## How It Works

### Block Structure

Every action in the system creates one new block:

```
Block #142
├── BlockIndex   : 142                          (sequential, never gaps)
├── PreviousHash : "000a3f9c2d7e..."             (SHA-256 hash of Block #141)
├── Hash         : "000d7b19e4f1..."             (SHA-256 of this block's contents)
├── Timestamp    : 2026-04-22T11:43:00Z          (UTC)
├── Action       : "Medication dispensed: Amoxicillin 500mg — 10 capsules (stock: 80 → 70)"
├── UserId       : "a1b2c3d4-..."                (ASP.NET Identity user ID)
├── UserName     : "Dr. Mokoena"
├── Role         : "Doctor, Pharmacist"
├── Category     : "Medication"                  (event type)
├── Data         : {"medicationId":5,"medicationName":"Amoxicillin 500mg","quantity":10}
├── Nonce        : 4821                          (proof-of-work nonce)
└── Signature    : "3f9a12b4c7d8e2f1"            (HMAC-SHA256 first 16 chars)
```

### Cryptographic Chain

Each block's hash is computed as:

```
SHA-256( BlockIndex + PreviousHash + Timestamp + Data + UserId + Action + Nonce )
```

Because every block includes the **previous block's hash**, changing any historical record breaks every subsequent hash — making tampering immediately detectable via `ValidateChainAsync()`.

---

## Proof of Work (Mining)

Before a block is saved, the system **mines** it by incrementing the `Nonce` until the SHA-256 hash starts with `"000"` (3 leading zeros):

```csharp
do {
    block.Nonce++;
    block.Hash = ComputeHash(block);
} while (!block.Hash.StartsWith("000"));
```

This means:
- On average ~4096 hash computations per block
- Each block has a valid proof-of-work certificate
- Reforging the chain requires recomputing every subsequent block — infeasible at scale

---

## HMAC Signature

After mining, each block's final hash is signed with **HMAC-SHA256** using a server-side secret key:

```
Signature = HMAC-SHA256( Hash, ServerSecret )[0..16]
```

This adds a second layer — even if someone with database access recomputes valid hashes, they cannot regenerate the correct HMAC signature without the server secret key stored in `appsettings.json` (`Blockchain:HmacSecret`).

---

## Event Categories

| Category       | Icon | Triggered By                                      |
|----------------|------|---------------------------------------------------|
| Medication     | 💊   | Dispense, stock adjust, create, edit              |
| Prescription   | 📝   | Prescription created, dispensed, cancelled        |
| Procurement    | 🛒   | Purchase requisition, purchase order, GRN         |
| UserManagement | 👤   | User created, password reset, role change         |
| HelpDesk       | 🎫   | Ticket created, status changed                    |
| System         | ⚙    | App startup, genesis block, system events         |

---

## Medication Management — Blockchain Events

Every medication action creates a dedicated blockchain block:

### Medication Dispensed
```json
{
  "medicationId": 5,
  "medicationName": "Amoxicillin 500mg",
  "quantity": 10,
  "notes": "Ward B, Patient: Sipho Dlamini"
}
```
**Action:** `"Medication dispensed: Amoxicillin 500mg — 10 capsules (stock: 80 → 70)"`

### Stock Adjusted
```json
{
  "medicationId": 5,
  "medicationName": "Amoxicillin 500mg",
  "quantity": 100,
  "notes": "Goods received from supplier"
}
```
**Action:** `"Stock added: Amoxicillin 500mg — 100 capsules (stock: 70 → 170) | Reason: Goods received from supplier"`

### Prescription Dispensed
```json
{
  "rxNumber": "RX00042",
  "patientName": "Sipho Dlamini",
  "medicationName": "Amoxicillin 500mg",
  "quantity": 21
}
```
**Action:** `"Prescription dispensed: RX00042 — Amoxicillin 500mg × 21 for Sipho Dlamini (stock: 170 → 149)"`

---

## Chain Validation

The Auditor/Admin role can view the blockchain explorer at `/Admin/Blockchain`.

The page displays:
- **Chain Valid / Chain Compromised** status badge (runs `ValidateChainAsync()` on every page load)
- Every block with its category badge, hash (leading `000` highlighted), nonce, HMAC signature snippet
- Colour-coded by event category for quick visual scanning
- Pagination (20 blocks per page, newest first)

`ValidateChainAsync()` checks every block in order:
1. Recomputes the hash and compares to the stored hash
2. Verifies `PreviousHash` matches the actual previous block's hash
3. Returns `false` at the first invalid block

---

## Limitations & Future Improvements

| Current State | Future Enhancement |
|---|---|
| Single-node (one database) | Distribute chain across department nodes |
| DB admin can delete records | Use append-only immutable ledger (e.g. Azure Immutable Blob) |
| Difficulty "000" (~4096 hashes) | Adjustable difficulty via configuration |
| HMAC secret in appsettings | Use Azure Key Vault / HSM for secret storage |
| Internal audit only | Submit chain root hash to public blockchain (timestamping) |

For a hospital internal audit system, the current implementation provides strong **application-level tamper evidence** — sufficient to detect unauthorized changes by any user operating through the application, and to satisfy internal audit and compliance requirements.

---

## Files

| File | Purpose |
|------|---------|
| `Models/BlockchainBlock.cs` | Block data model |
| `Services/BlockchainService.cs` | Mining, hashing, signing, validation |
| `Services/IBlockchainService.cs` | Service interface |
| `Services/AuditService.cs` | Caller-aware audit wrapper |
| `Services/IAuditService.cs` | Audit interface with typed medication methods |
| `Controllers/MedicationController.cs` | Medication CRUD + dispense + stock adjust |
| `Controllers/PrescriptionController.cs` | Prescription create + dispense + cancel |
| `Views/Admin/Blockchain.cshtml` | Blockchain explorer UI |
| `Views/Admin/AuditTrail.cshtml` | Flat audit log table |
| `Views/Medication/` | Medication management views |
| `Views/Prescription/` | Prescription management views |
