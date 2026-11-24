# Webshop Security Architecture

## 1. Overview

This document describes the security architecture of the Webshop application, focusing on the protection of user accounts, customer data, checkout operations, and simulated payment processing.

The application uses a layered security approach to reduce the risk of common web application vulnerabilities.

The payment system is a demonstration environment. It uses test card details and simulated 3D Secure verification. It does not process real payments.

---

## 2. Payment Verification Protection

### Level 1 — Order-Based Lockout

**Status: Implemented**

- Maximum of 5 incorrect 3D Secure verification attempts per order.
- After 5 failed attempts, the order is locked for 15 minutes.
- Correct verification codes are rejected while the order is locked.
- Expired locks reset the attempt counter.
- Successful payment removes the order-specific attempt record.

### Level 2 — User-Based Lockout

**Status: Implemented**

- Maximum of 10 incorrect verification attempts per user within a 15-minute window.
- After reaching the limit, payment verification is blocked for that user for 15 minutes.
- The restriction applies across different orders.
- Creating another order does not bypass the user-level restriction.
- Successful payment does not reset the user-level counter prematurely.

### Level 3 — HTTP Request Rate Limiting

**Status: Implemented**

- Payment POST requests are limited to 10 requests per minute.
- Requests are partitioned using the authenticated user ID and IP address.
- GET requests are excluded from this policy.
- Excessive requests receive HTTP 429 Too Many Requests.

---

## 3. Access Control and IDOR Protection

**Status: Code Review and Practical Tests Completed for Customer Pages**

The application checks resource ownership using the authenticated user's ID.

Reviewed components:

- My Orders
- Order Details
- Checkout
- Payment
- Payment Confirmation
- Cart

Important protections:

- Orders are queried using both the order ID and authenticated user ID.
- Payment operations verify that the order belongs to the current user.
- Cart operations verify cart ownership.
- Confirmation pages only display the current user's paid orders.
- Unauthorized access to another user's order returns HTTP 404.

### Practical Testing

- Changing the order ID in My Orders resulted in HTTP 404.
- Changing the order ID in Confirmation resulted in HTTP 404.

**Admin Access Control is outside the scope of this review.**

---

## 4. SQL Injection Protection

**Status: Code Review Completed — Practical Testing Pending**

The reviewed application components use Entity Framework Core LINQ queries instead of manually constructed SQL statements.

Reviewed components:

- Checkout/Index.cshtml.cs
- Products/Index.cshtml.cs
- Cart/Index.cshtml.cs
- CartService.cs
- ApplicationDbContext.cs

Security observations:

- EF Core parameterizes values used in LINQ queries.
- No unsafe raw SQL construction was identified in the reviewed files.
- No FromSqlRaw() or ExecuteSqlRaw() usage was identified in the reviewed files.
- Database relationships and constraints are configured through Fluent API.
- Unique indexes protect important identifiers and relationships.

A full project-wide search for raw SQL and practical SQL Injection testing remain pending.

---

## 5. HTTP Security Headers

**Status: Implemented — Initial Configuration Tested**

The application sends the following HTTP security headers:

- X-Content-Type-Options: nosniff
- Referrer-Policy: strict-origin-when-cross-origin
- Permissions-Policy: camera=(), microphone=(), geolocation=()
- X-Frame-Options: DENY
- Content-Security-Policy

The initial Content Security Policy includes:

- default-src 'self'
- script-src 'self' 'unsafe-inline'
- style-src 'self' 'unsafe-inline'
- img-src 'self' data: blob:
- font-src 'self' data:
- connect-src 'self'
- object-src 'none'
- base-uri 'self'
- form-action 'self'
- frame-ancestors 'none'

The headers were verified in browser Developer Tools.

The CSP is an initial compatibility configuration and still permits inline scripts and styles. A stricter policy may be introduced later.

---

## 6. Further Security Improvements

### Race Condition Protection

**Status: Planned — Deferred**

Potential concurrent operations include:

- Simultaneous cart updates.
- Simultaneous payment verification requests.
- Duplicate payment processing.

Database constraints provide some protection, but additional transactional and concurrency safeguards may be required.

### Payment Integrity and Duplicate Prevention

**Status: Planned**

Further review of payment idempotency, duplicate submissions, and concurrent payment processing is required.

### Authentication and Account Security

**Status: Planned — Review Deferred**

Review account creation, password policies, email verification, password reset, and account-related security settings.

### Database Security

**Status: Initial Configuration Reviewed — Further Review Planned**

Review database permissions, connection string protection, backups, and production database configuration.

### OWASP Vulnerability Review

**Status: In Progress**

Completed:
- Customer-side access control review.
- Practical IDOR tests.
- Initial SQL Injection code review.

Pending:
- Project-wide SQL query review and practical testing.
- XSS review.
- CSRF review.
- File upload security review.
- Final security testing.

### Secrets and Logging

**Status: Planned**

Review secret storage, sensitive information in logs, and exception handling.

---

## 7. Implementation Roadmap

| Security Feature | Status |
|---|---|
| Order-Based Lockout | Implemented |
| User-Based Lockout | Implemented |
| HTTP Request Rate Limiting | Implemented |
| Race Condition Protection | Deferred |
| Payment Integrity and Duplicate Prevention | Planned |
| Authentication Review | Deferred |
| Database Security Review | Partially Reviewed |
| OWASP Vulnerability Review | In Progress |
| HTTP Security Headers | Implemented — Initial Configuration |
| Secrets and Logging | Planned |
| Final Testing and Documentation | Pending |

---

## 8. Implementation and Testing Log

### Payment Protection
- Implemented order-based verification lockout.
- Implemented user-based verification lockout.
- Implemented HTTP request rate limiting.
- Tested simulated successful and failed verification scenarios.

### HTTP Security Headers
- Added initial security header middleware.
- Verified all five security headers in browser Developer Tools.
- Confirmed that the website continued functioning.

### Access Control Review
- Reviewed customer order ownership checks.
- Tested unauthorized access to another user's order.
- Tested unauthorized access to another user's confirmation page.
- Both tests returned HTTP 404 as expected.

### SQL Injection Review
- Reviewed Checkout, Products, Cart, CartService, and ApplicationDbContext.
- Confirmed that the reviewed queries use EF Core LINQ.
- No obvious SQL Injection vulnerabilities were identified in the reviewed files.
- Practical testing and project-wide raw SQL search remain pending.

---

## 9. Important Payment Security Notes

The application uses test payment data only.

- No full card numbers are stored.
- No CVV values are stored.
- Only safe card metadata, such as card brand and last four digits, is retained.
- The 3D Secure process is simulated and does not represent real bank authentication.
- The application does not claim PCI DSS compliance.