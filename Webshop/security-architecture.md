# Webshop Security Architecture

## 1. Overview

This document describes the security architecture of the Webshop application, focusing on the protection of the checkout and payment processes.

The main objective is to protect user accounts, payment operations, and customer data against common security threats, including brute-force attacks, unauthorized access, and attempts to bypass payment verification.

The application uses a multi-layered security approach. Each layer addresses a different type of threat, and together they provide stronger protection than relying on a single security mechanism.

The security architecture is being extended gradually. Each mechanism is implemented, tested, and documented before moving to the next stage.

---

## 2. Payment Security Levels

### Level 1 — Order-Based Payment Verification Lockout

**Status: Implemented**

#### Purpose

This level protects individual orders against brute-force attacks targeting the simulated 3D Secure verification process.

Without an attempt limit, an attacker could repeatedly submit different verification codes until the correct code is discovered.

#### How It Works

Each order has its own verification attempt counter stored in the `PaymentVerificationAttempts` database table.

The system applies the following rules:

- A maximum of 5 incorrect verification attempts is allowed.
- After the fifth incorrect attempt, verification for that order is locked for 15 minutes.
- Correct verification codes are rejected while the order is locked.
- After the lockout period expires, the attempt counter is reset.
- Successful payment removes the corresponding verification attempt record.

The lockout is associated with the order ID (`OrderId`).

#### Security Benefit

This mechanism prevents unlimited verification attempts against a single order.

However, it does not prevent an attacker from creating another order and attempting verification again. This limitation is addressed by Level 2.

---

### Level 2 — User-Based Payment Verification Lockout

**Status: Implemented**

#### Purpose

This level extends payment protection from individual orders to the entire user account.

It prevents users from bypassing the order-based lockout by creating new orders.

#### How It Works

A separate database table, `UserPaymentVerificationAttempts`, maintains verification attempt information for each user.

The system applies the following rules:

- A maximum of 10 incorrect verification attempts within a 15-minute window.
- After reaching the limit, payment verification is temporarily blocked for the entire user account.
- The restriction applies to all orders belonging to that user.
- Creating a new order does not reset or bypass the account-level lockout.
- After the lockout expires, the attempt counter is reset.
- The account-level counter is retained after successful payment until its counting window expires.

The counter is associated with the user's ID (`UserId`), rather than an individual order.

#### Security Benefit

This mechanism prevents repeated brute-force attempts across multiple orders belonging to the same account.

It complements Level 1 by introducing a broader protection scope.

---

### Level 3 — HTTP Request Rate Limiting

**Status: Planned — Next Implementation Stage**

#### Purpose

This level protects the application against excessive HTTP requests targeting payment-related endpoints.

Unlike the previous levels, which track incorrect verification codes, this mechanism limits how frequently requests can reach the application.

#### How It Will Work

ASP.NET Core Rate Limiting middleware will be configured to restrict the frequency of requests to payment-related endpoints.

The planned protection includes:

- Limiting the number of requests within a specified time window.
- Applying limits based on the authenticated user and IP address.
- Rejecting excessive requests with HTTP status code `429 Too Many Requests`.
- Preventing automated clients from submitting requests at an unrestricted rate.

The exact limits will be determined during implementation and testing.

#### Security Benefit

This mechanism adds protection against automated attacks and excessive request traffic.

It also provides an additional barrier against attempts to bypass the first two protection levels.

---

## 3. Layered Security Strategy

The three levels operate at different scopes:

| Security Level | Protection Scope | Main Purpose |
|---|---|---|
| Level 1 | Individual order | Prevent brute-force attempts against one order |
| Level 2 | User account | Prevent bypassing lockouts by creating new orders |
| Level 3 | HTTP requests | Limit excessive requests reaching payment endpoints |

Using these mechanisms together provides multiple independent security controls.

No single layer is considered sufficient on its own.

---

## 4. Further Security Improvements

After implementing the three payment security levels, the following improvements are planned.

### 4.1. Race Condition Protection

**Status: Planned**

Protect payment verification counters and payment operations against simultaneous requests.

Planned improvements:

- Prevent concurrent requests from incorrectly updating verification attempt counters.
- Ensure that simultaneous payment requests cannot process the same order more than once.
- Review database transactions and atomic operations.
- Strengthen duplicate payment prevention.

### 4.2. Payment Integrity and Duplicate Prevention

**Status: Planned**

Further strengthen payment processing to ensure that:

- An order cannot be paid more than once.
- Payment status and order status remain consistent.
- Repeated or replayed requests cannot create duplicate successful payments.
- Database constraints support application-level checks.

### 4.3. Authentication and Account Security

**Status: Planned**

Review and strengthen authentication-related protections, including:

- Email confirmation.
- Login brute-force protection.
- Password recovery security.
- Session expiration and invalidation.
- Protection of sensitive account operations.

### 4.4. Database Security

**Status: Planned**

Review database security and data integrity, including:

- Foreign key constraints and indexes.
- Appropriate database user permissions.
- Secure connection string storage.
- Validation of database operations and transactions.
- Protection of personal and payment-related data.

### 4.5. OWASP Security Review

**Status: Planned**

Review the application against common web application vulnerabilities, including:

- SQL Injection.
- Cross-Site Scripting (XSS).
- Cross-Site Request Forgery (CSRF).
- Broken Access Control and IDOR.
- Unsafe file uploads.

### 4.6. HTTP Security Headers

**Status: Planned**

Configure additional HTTP security headers, including:

- Content-Security-Policy (CSP).
- X-Content-Type-Options.
- Referrer-Policy.
- Permissions-Policy.
- Appropriate clickjacking protection.

### 4.7. Secrets and Logging

**Status: Planned**

Improve the handling of sensitive configuration and security events:

- Store SMTP credentials and connection strings securely.
- Avoid exposing secrets in source code or logs.
- Log relevant security events, including failed verification attempts and account lockouts.
- Never log full card numbers, CVV values, passwords, or verification codes.

---

## 5. Implementation Roadmap

The planned implementation order is:

| Step | Task | Status |
|---|---|---|
| 1 | Order-based payment verification lockout | Implemented |
| 2 | User-based payment verification lockout | Implemented |
| 3 | HTTP Request Rate Limiting | Next |
| 4 | Race condition protection | Planned |
| 5 | Duplicate payment and payment integrity protection | Planned |
| 6 | Authentication and account security review | Planned |
| 7 | Database security review | Planned |
| 8 | OWASP vulnerability review | Planned |
| 9 | HTTP security headers | Planned |
| 10 | Secrets management and security logging | Planned |
| 11 | Final security testing and documentation review | Planned |

Each stage will be implemented and tested before proceeding to the next one.

---

## 6. Implementation and Testing Log

This section records the current implementation status.

| Component | Status |
|---|---|
| Order-based verification lockout | Implemented |
| User-based verification lockout | Implemented |
| HTTP request rate limiting | Planned |
| Race condition protection | Planned |
| Duplicate payment prevention improvements | Planned |
| Authentication security review | Planned |
| Database security review | Planned |
| OWASP security review | Planned |
| HTTP security headers | Planned |
| Secrets management and security logging | Planned |
| Final security testing | Planned |

Further implementation details, configuration decisions, and testing results will be documented as the project develops.