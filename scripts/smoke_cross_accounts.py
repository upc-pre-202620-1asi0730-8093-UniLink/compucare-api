#!/usr/bin/env python3
"""Development-only PostgreSQL/JWT smoke test for the four CompuCare account roles."""
import json
import os
import time
import urllib.error
import urllib.request

BASE = os.getenv("BASE_URL", "http://127.0.0.1:5147/api/v1")
PASSWORD = os.environ["DemoSeed__Password"]

def api(path, token=None, method="GET", data=None, expected=(200,)):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(
        BASE + path, headers=headers, method=method,
        data=json.dumps(data).encode("utf-8") if data is not None else None)
    try:
        with urllib.request.urlopen(request, timeout=20) as response:
            status, raw = response.status, response.read()
    except urllib.error.HTTPError as exc:
        status, raw = exc.code, exc.read()
    assert status in expected, f"{method} {path}: expected {expected}, received {status}: {raw[:500]}"
    return json.loads(raw) if raw else None

for attempt in range(70):
    try:
        with urllib.request.urlopen(BASE.replace("/api/v1", "/health"), timeout=2):
            break
    except (urllib.error.URLError, TimeoutError):
        time.sleep(2)
else:
    raise RuntimeError("CompuCare API did not start within the health-check window")

def login(email):
    response = api("/auth/login", method="POST", data={"email": email, "password": PASSWORD})
    return response["token"], response["user"]["id"]

employee, employee_id = login("empleada@mype.pe")
technician, technician_id = login("tecnico@unilink.pe")
manager, _ = login("admin@mype.pe")
sysadmin, _ = login("soporte@unilink.pe")

equipment = api("/equipments", employee)
assert equipment, "Development-seeded employee must have assigned equipment"
created = api("/tickets", employee, "POST", {
    "equipmentId": equipment[0]["id"], "category": "Hardware",
    "description": "The office desktop does not start when pressing the power button."}, expected=(201,))
ticket_id = created["id"]
api(f"/tickets/{ticket_id}/assign", employee, "PUT",
    {"technicianId": technician_id}, expected=(403,))
assert any(t["id"] == ticket_id for t in api("/tickets", sysadmin))
api(f"/tickets/{ticket_id}/assign", sysadmin, "PUT", {"technicianId": technician_id})
assert any(t["id"] == ticket_id for t in api("/tickets", technician))
api(f"/tickets/{ticket_id}/start", technician, "PUT")

quote = api("/quotes", technician, "POST", {"ticketId": ticket_id, "items": [
    {"description": "Replacement power supply unit", "cost": 20.5}]}, expected=(201,))
quote_id = quote["id"]
api(f"/quotes/{quote_id}/status", employee, "PUT", {"status": "APPROVED"}, expected=(403,))
diagnosis = {"diagnosis": "Power supply failure", "workDone": "Replaced the power supply", "hours": 1}
api(f"/tickets/{ticket_id}/diagnosis", technician, "PUT", diagnosis, expected=(400, 409))
api(f"/quotes/{quote_id}/status", manager, "PUT", {"status": "APPROVED"})
api(f"/tickets/{ticket_id}/diagnosis", technician, "PUT", diagnosis)
api(f"/tickets/{ticket_id}/diagnosis", technician, "PUT", diagnosis, expected=(400, 409))
assert any(t["id"] == ticket_id and t["status"] == "Atendido" for t in api("/tickets", employee))
assert api("/subscriptions/balance", manager)["hoursUsed"] == 1
print("PASS: BC01 login, BC02 equipment, BC04 cross-role assignment, BC05 approval, BC03 coverage.")
