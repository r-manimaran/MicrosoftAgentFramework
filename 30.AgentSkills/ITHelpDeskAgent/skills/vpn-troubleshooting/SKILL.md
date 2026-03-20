---
name: vpn-troubeshooting
description: >-
	Diagnose and resolve VPN connection issues for remote employees.
	Use when asked about VPN not connecting, slow VPN speeds, authentication erros,
	or certificate problems.
metadata:
	author: network-team
	version: "2.0"
---

## Instructions

1. Ask the employee for their OS (Windows/macOS/Linux) and VPN client version.
2. Check if they are using the correct VPN server - read `references/VPN_SERVERS.md`
   to confirm the right endpoint for their region.
3. Run through the standard diagnostic checklist:
   - Can they ping 8.8.8.8? (basic internet check)
   - Is the Cisco AnyConnect client updated to v4.10+?
   - Have they tried disconnecting and reconnecting?

4. For certificate errors, instruct them to re-dowload the certificate bundle from the IT portal.
5. If unresolved after 3 steps, create a ticket in ServiceNow with tag VPN-ISSUE.