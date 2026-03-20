# Contoso IT Security — Password & Account Policy
Version: 3.2 | Last Updated: March 2026 | Owner: it-security@contoso.com

## Password Requirements

- Minimum 12 characters
- Must include: uppercase, lowercase, number, and special character (!@#$%^&*)
- Cannot reuse the last 10 passwords
- Expires every 90 days — users receive email reminders at 14, 7, and 1 day before expiry
- Cannot contain the user's display name or username

## Account Lockout Rules

| Event                        | Threshold | Lockout Duration     |
|------------------------------|-----------|----------------------|
| Failed login attempts        | 5 in a row | 30 minutes (auto-unlock) |
| Failed MFA attempts          | 3 in a row | 60 minutes (auto-unlock) |
| Repeated lockouts (same day) | 3 events  | Manual unlock required   |

## Self-Service Reset (Preferred)

1. Navigate to https://resetpw.contoso.com
2. Enter corporate email address
3. Choose verification method: SMS to registered mobile, or backup email
4. Answer 2 of 3 registered security questions
5. Set a new password meeting the requirements above
6. Password takes effect immediately; active sessions are terminated

**Note:** Self-service is available 24/7 and does not require IT involvement.

## Manual Reset Procedure (IT Agent Use Only)

Use this procedure when the self-service portal is unavailable or the 
employee cannot access their registered phone/email.

### Identity Verification (Required — do not skip)

IT agents must verify identity using TWO of the following:
- [ ] Employee ID (6-digit number on badge)
- [ ] Date of birth
- [ ] Manager's full name
- [ ] Office location + floor number
- [ ] Last 4 digits of corporate device serial number

### Reset Steps

1. Log into the IT Admin Portal at https://admin.contoso.com
2. Navigate to **Users → Search** and locate the account by email
3. Click **Reset Password** → select **Force change on next login**
4. Generate a temporary password using the built-in generator (min 16 chars)
5. Deliver temporary password via **phone call only** (never email or chat)
6. Unlock account if locked: click **Unlock Account** on the same user page
7. Log the action in ServiceNow under category: `IDENTITY-RESET`
8. Ticket auto-closes after confirmed successful login within 4 hours

### Escalation

If an account shows signs of compromise (unusual login location, login time 
outside work hours, multiple failed MFA attempts from different IPs):
- Do NOT reset password immediately
- Tag ticket: `SEC-COMPROMISE` and assign to security@contoso.com
- Disable the account temporarily via: Users → [Account] → **Disable**
- Notify the employee's manager by phone

## MFA (Multi-Factor Authentication)

**Supported methods (in order of security preference):**
1. Microsoft Authenticator app (push notification)
2. Hardware TOTP token (YubiKey — issued on request)
3. SMS to registered mobile number
4. Backup code (one-time use — 10 codes issued at enrollment)

**MFA Reset:**
- Employee can reset via https://mfa.contoso.com if they have an active session
- If locked out of MFA: IT agent must raise a ticket tagged `MFA-RESET`
  and CC security@contoso.com — approval from manager required within 24hrs
- Hardware tokens: contact it-procurement@contoso.com for replacement

## Privileged Accounts (Admin/Service Accounts)

- Separate password policy: 16-char minimum, 30-day rotation
- Resets require TWO IT admins to approve via the PAM portal (https://pam.contoso.com)
- Service account resets must be coordinated with the owning team
  to avoid breaking dependent services

## Compliance & Audit

- All password resets are logged with timestamp, agent ID, and justification
- Logs retained for 12 months per ISO 27001 requirements
- Monthly audit report sent to CISO and department heads
- Employees may request their own reset history via privacy@contoso.com