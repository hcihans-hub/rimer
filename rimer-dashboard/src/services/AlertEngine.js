export class AlertEngine {
  constructor() {
    this.activeAlerts = new Map();
    this.lastSystemMode = 'NORMAL';
  }

  Evaluate(incomingAlerts = [], systemMode = 'NORMAL') {
    const addedAlerts = [];
    const removedAlerts = [];
    
    // Quick lookup for incoming alert IDs
    const incomingIds = new Set(incomingAlerts.map(a => a.id));

    // Find newly added alerts
    for (const alert of incomingAlerts) {
      if (!this.activeAlerts.has(alert.id)) {
        addedAlerts.push(alert);
        this.activeAlerts.set(alert.id, alert);
      }
    }

    // Find removed alerts
    for (const [id] of this.activeAlerts) {
      if (!incomingIds.has(id)) {
        removedAlerts.push(id);
        this.activeAlerts.delete(id);
      }
    }

    const systemModeChanged = systemMode !== this.lastSystemMode;
    this.lastSystemMode = systemMode;

    // Return NEW object to prevent mutation bugs and async corruption
    return {
      addedAlerts: [...addedAlerts],
      removedAlerts: [...removedAlerts],
      systemMode,
      systemModeChanged
    };
  }
}

export const alertEngine = new AlertEngine();
