(() => {
  "use strict";

  const token = document.querySelector("#anti-forgery input")?.value ?? "";
  const toastViewport = document.getElementById("toast-viewport");

  function notify(message, tone = "info") {
    if (!toastViewport) return;
    const item = document.createElement("div");
    item.className = `toast ${tone}`;
    item.setAttribute("role", "status");
    item.textContent = message;
    toastViewport.append(item);
    window.setTimeout(() => item.remove(), 4200);
  }

  async function request(url, options = {}) {
    const response = await fetch(url, {
      ...options,
      headers: {
        Accept: "application/json",
        ...(options.body ? { "Content-Type": "application/json" } : {}),
        ...(options.method && options.method !== "GET" ? { RequestVerificationToken: token } : {}),
        ...options.headers,
      },
    });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(payload.message || payload.title || "The operation could not be completed.");
    return payload;
  }

  const sidebar = document.getElementById("sidebar");
  const menuToggle = document.getElementById("menu-toggle");
  menuToggle?.addEventListener("click", () => {
    const open = sidebar?.classList.toggle("open") ?? false;
    menuToggle.setAttribute("aria-expanded", String(open));
  });
  document.addEventListener("click", (event) => {
    if (window.innerWidth > 780 || !sidebar?.classList.contains("open")) return;
    if (!sidebar.contains(event.target) && !menuToggle?.contains(event.target)) {
      sidebar.classList.remove("open");
      menuToggle?.setAttribute("aria-expanded", "false");
    }
  });

  const environment = document.getElementById("environment");
  if (environment) {
    environment.value = sessionStorage.getItem("evostel-environment") || "UAT";
    environment.addEventListener("change", () => {
      sessionStorage.setItem("evostel-environment", environment.value);
      notify(`Environment view changed to ${environment.value}.`, "info");
    });
  }

  document.querySelectorAll("[data-open-dialog]").forEach((trigger) => {
    trigger.addEventListener("click", () => document.getElementById(trigger.dataset.openDialog)?.showModal());
  });
  document.querySelectorAll("[data-close-dialog]").forEach((trigger) => {
    trigger.addEventListener("click", () => trigger.closest("dialog")?.close());
  });
  document.querySelectorAll("dialog").forEach((dialog) => {
    dialog.addEventListener("click", (event) => {
      if (event.target === dialog) dialog.close();
    });
  });

  const serviceDialog = document.getElementById("service-dialog");
  document.querySelectorAll("[data-service-node]").forEach((node) => {
    node.addEventListener("click", () => {
      document.getElementById("service-dialog-title").textContent = node.dataset.name;
      const body = document.getElementById("service-dialog-body");
      body.replaceChildren();
      const wrapper = document.createElement("div");
      wrapper.className = "service-details";
      const status = document.createElement("span");
      status.className = `status status-${node.dataset.status}`;
      status.textContent = node.dataset.status;
      const grid = document.createElement("dl");
      grid.className = "detail-grid";
      [["Response time", `${node.dataset.latency} ms`], ["Availability", `${node.dataset.availability}%`], ["Owner", node.dataset.owner], ["Source", node.dataset.source]].forEach(([label, value]) => {
        const row = document.createElement("div");
        const dt = document.createElement("dt");
        const dd = document.createElement("dd");
        dt.textContent = label;
        dd.textContent = value;
        row.append(dt, dd);
        grid.append(row);
      });
      const detail = document.createElement("p");
      detail.textContent = node.dataset.detail;
      wrapper.append(status, grid, detail);
      body.append(wrapper);
      serviceDialog?.showModal();
    });
  });

  let activeIncidentRow = null;
  const incidentDialog = document.getElementById("incident-dialog");
  document.querySelectorAll(".incident-view").forEach((button) => {
    button.addEventListener("click", () => {
      activeIncidentRow = button.closest("[data-incident-row]");
      const data = activeIncidentRow.dataset;
      document.getElementById("incident-dialog-title").textContent = `${data.id} — ${data.title}`;
      const body = document.getElementById("incident-dialog-body");
      body.replaceChildren();
      const summary = document.createElement("p");
      summary.textContent = data.summary;
      const grid = document.createElement("dl");
      grid.className = "detail-grid";
      [["Severity", data.severity], ["Service", data.service], ["Owner", data.owner], ["Status", data.status], ["Opened", data.opened], ["SLA remaining", data.sla]].forEach(([label, value]) => {
        const row = document.createElement("div");
        const dt = document.createElement("dt");
        const dd = document.createElement("dd");
        dt.textContent = label;
        dd.textContent = value;
        row.append(dt, dd);
        grid.append(row);
      });
      body.append(summary, grid);
      incidentDialog?.showModal();
    });
  });

  document.querySelector(".acknowledge-incident")?.addEventListener("click", async (event) => {
    if (!activeIncidentRow) return;
    const button = event.currentTarget;
    button.disabled = true;
    button.textContent = "Acknowledging…";
    try {
      const incident = await request(`/api/operations/incidents/${encodeURIComponent(activeIncidentRow.dataset.id)}/acknowledge`, { method: "POST" });
      activeIncidentRow.dataset.status = incident.status;
      const status = activeIncidentRow.querySelector(".status");
      status.className = `status status-${incident.status}`;
      status.textContent = incident.status;
      incidentDialog.close();
      notify(`${incident.id} acknowledged.`, "success");
    } catch (error) {
      notify(error.message, "error");
    } finally {
      button.disabled = false;
      button.textContent = "Acknowledge incident";
    }
  });

  const search = document.getElementById("incident-search");
  const severity = document.getElementById("severity-filter");
  const incidentStatus = document.getElementById("status-filter");
  function filterIncidents() {
    const query = search?.value.trim().toLowerCase() || "";
    let visible = 0;
    document.querySelectorAll("[data-incident-row]").forEach((row) => {
      const matchesText = !query || `${row.dataset.id} ${row.dataset.title} ${row.dataset.service} ${row.dataset.owner}`.toLowerCase().includes(query);
      const matchesSeverity = !severity?.value || row.dataset.severity === severity.value;
      const matchesStatus = !incidentStatus?.value || row.dataset.status === incidentStatus.value;
      row.hidden = !(matchesText && matchesSeverity && matchesStatus);
      if (!row.hidden) visible += 1;
    });
    const count = document.getElementById("incident-count");
    if (count) count.textContent = String(visible);
    const empty = document.getElementById("incident-empty");
    if (empty) empty.hidden = visible !== 0;
  }
  [search, severity, incidentStatus].forEach((control) => control?.addEventListener("input", filterIncidents));
  function clearFilters() {
    if (search) search.value = "";
    if (severity) severity.value = "";
    if (incidentStatus) incidentStatus.value = "";
    filterIncidents();
    search?.focus();
  }
  document.getElementById("clear-search")?.addEventListener("click", clearFilters);
  document.getElementById("empty-clear")?.addEventListener("click", clearFilters);

  async function runHealthProbe() {
    const buttons = [document.getElementById("refresh-health"), document.getElementById("page-refresh")].filter(Boolean);
    buttons.forEach((button) => { button.disabled = true; button.setAttribute("aria-busy", "true"); });
    try {
      const result = await request("/api/operations/health");
      const checkedAt = new Date(result.checkedAt);
      const time = Number.isNaN(checkedAt.getTime()) ? "Just now" : checkedAt.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
      const lastChecked = document.getElementById("last-checked");
      if (lastChecked) lastChecked.textContent = time;
      const banner = document.getElementById("platform-banner");
      if (banner) {
        banner.className = `system-banner ${result.state}`;
        banner.querySelector(".banner-icon").textContent = result.state === "healthy" ? "✓" : "!";
        document.getElementById("platform-title").textContent = result.state === "healthy" ? "Platform operating normally" : result.state === "degraded" ? "Platform performance is degraded" : "Platform requires attention";
        document.getElementById("health-message").textContent = result.message;
      }
      document.querySelectorAll('[data-service-node][data-name="Commercial API"]').forEach((node) => {
        node.classList.remove("healthy", "degraded", "unavailable");
        node.classList.add(result.state);
        node.dataset.status = result.state;
        node.dataset.latency = result.latency ?? "—";
        node.querySelector("[data-latency]").textContent = result.latency == null ? "—" : `${result.latency} ms`;
        node.querySelector("[data-status]").textContent = result.state;
      });
      const row = document.querySelector('[data-service-row="commercial-api"]');
      if (row) {
        row.querySelector("[data-latency]").textContent = result.latency == null ? "—" : `${result.latency} ms`;
        const status = row.querySelector(".status");
        status.className = `status status-${result.state}`;
        status.textContent = result.state;
      }
      if (result.state !== "healthy") notify(result.message, result.state === "unavailable" ? "error" : "info");
    } catch (error) {
      const lastChecked = document.getElementById("last-checked");
      if (lastChecked) lastChecked.textContent = "Unavailable";
      notify(error.message, "error");
    } finally {
      buttons.forEach((button) => { button.disabled = false; button.removeAttribute("aria-busy"); });
    }
  }
  document.getElementById("refresh-health")?.addEventListener("click", runHealthProbe);
  document.getElementById("page-refresh")?.addEventListener("click", runHealthProbe);
  runHealthProbe();

  document.querySelectorAll(".integration-check").forEach((button) => {
    button.addEventListener("click", async () => {
      button.disabled = true;
      button.textContent = "Checking…";
      try {
        const result = await request(`/api/operations/integrations/${encodeURIComponent(button.dataset.id)}/check`, { method: "POST" });
        const row = document.getElementById(`integration-${result.id}`);
        row.querySelector("[data-last-run]").textContent = result.lastRun;
        const status = row.querySelector("[data-result]");
        status.className = `status status-${result.result}`;
        status.textContent = result.result;
        notify(`${result.name} check completed.`, "success");
      } catch (error) {
        notify(error.message, "error");
      } finally {
        button.disabled = false;
        button.textContent = "Run check";
      }
    });
  });

  const ticketForm = document.getElementById("ticket-form");
  ticketForm?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const fields = Object.fromEntries(new FormData(ticketForm));
    const rules = { subject: [3, "Enter a subject with at least 3 characters."], customer: [2, "Enter the customer name."], description: [10, "Describe the issue using at least 10 characters."] };
    let firstInvalid = null;
    Object.entries(rules).forEach(([name, [minimum, message]]) => {
      const input = ticketForm.elements[name];
      const error = input.parentElement.querySelector(".field-error");
      const invalid = String(fields[name] || "").trim().length < minimum;
      input.setAttribute("aria-invalid", String(invalid));
      error.textContent = invalid ? message : "";
      if (invalid && !firstInvalid) firstInvalid = input;
    });
    if (firstInvalid) return firstInvalid.focus();
    const submit = ticketForm.querySelector('[type="submit"]');
    submit.disabled = true;
    submit.textContent = "Creating ticket…";
    try {
      const ticket = await request("/api/operations/tickets", { method: "POST", body: JSON.stringify(fields) });
      const row = document.createElement("tr");
      const addCell = (value, className) => {
        const cell = document.createElement("td");
        const content = document.createElement("span");
        content.textContent = value;
        if (className) content.className = className;
        cell.append(content);
        row.append(cell);
      };
      addCell(ticket.id); addCell(ticket.subject); addCell(ticket.customer); addCell(ticket.level, "tier-label"); addCell(ticket.age); addCell(ticket.owner); addCell(ticket.status, `status status-${ticket.status}`);
      document.getElementById("support-rows")?.prepend(row);
      ticketForm.reset();
      ticketForm.closest("dialog").close();
      notify(`${ticket.id} created and routed to ${ticket.level}.`, "success");
    } catch (error) {
      notify(error.message, "error");
    } finally {
      submit.disabled = false;
      submit.textContent = "Create ticket";
    }
  });

  const density = document.getElementById("density-setting");
  if (density) {
    density.value = localStorage.getItem("evostel-density") || "comfortable";
    document.body.classList.toggle("compact", density.value === "compact");
    density.addEventListener("change", () => {
      localStorage.setItem("evostel-density", density.value);
      document.body.classList.toggle("compact", density.value === "compact");
      notify(`Table density changed to ${density.value}.`, "success");
    });
  }
  document.getElementById("preferences-form")?.addEventListener("submit", (event) => {
    event.preventDefault();
    const values = Object.fromEntries(new FormData(event.currentTarget));
    sessionStorage.setItem("evostel-notifications", JSON.stringify(values));
    notify("Notification preferences saved for this session.", "success");
  });
})();
