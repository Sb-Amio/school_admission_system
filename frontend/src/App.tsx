
import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import { apiRequest } from "./services/api";
import "./App.css";

interface SchoolClass {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

interface Application {
  holdId: string;
  classId: string;
  className: string;
  status: string;
  expiresAt: string;
}

interface HoldResponse {
  holdId: string;
  classId: string;
  studentId: string;
  status: string;
  expiresAt: string;
}

type View = "student" | "admin";

const validStudentId = (id: string) => /^[0-9]{17}$/.test(id);

function getRemainingSeconds(date: string, now: number): number {
  return Math.max(
    0,
    Math.ceil((new Date(date).getTime() - now) / 1000)
  );
}

function formatCountdown(seconds: number): string {
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;

  return `${String(minutes).padStart(2, "0")}:${String(
    remainingSeconds
  ).padStart(2, "0")}`;
}

function formatDate(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Not available";
  }

  return date.toLocaleString();
}

function errorMessage(error: unknown): string {
  return error instanceof Error
    ? error.message
    : "An unexpected error occurred.";
}

function App() {
  const [view, setView] = useState<View>("student");
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [selectedClassId, setSelectedClassId] = useState("");
  const [studentId, setStudentId] = useState("");
  const [applications, setApplications] = useState<Application[]>([]);
  const [className, setClassName] = useState("");
  const [totalSeats, setTotalSeats] = useState("");
  const [now, setNow] = useState(Date.now());
  const [busy, setBusy] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [applicationsLoading, setApplicationsLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [refreshError, setRefreshError] = useState("");

  const studentIsValid = validStudentId(studentId);

  const selectedClass = useMemo(
    () => classes.find((item) => item.id === selectedClassId),
    [classes, selectedClassId]
  );

  const totalAvailable = classes.reduce(
    (sum, item) => sum + item.availableSeats,
    0
  );

  const totalConfirmed = classes.reduce(
    (sum, item) => sum + item.confirmedSeats,
    0
  );

  const activeApplications = applications.filter(
    (item) =>
      item.status === "HELD" &&
      getRemainingSeconds(item.expiresAt, now) > 0
  ).length;

  async function fetchClasses(): Promise<SchoolClass[]> {
    return apiRequest<SchoolClass[]>("/classes");
  }

  async function fetchApplications(id: string): Promise<Application[]> {
    return apiRequest<Application[]>("/me/holds", {
      headers: {
        "X-Student-Id": id,
      },
    });
  }

  async function refreshData(id: string) {
    const classData = await fetchClasses();
    setClasses(classData);

    if (validStudentId(id)) {
      const applicationData = await fetchApplications(id);
      setApplications(applicationData);
    } else {
      setApplications([]);
    }
  }

  useEffect(() => {
    let active = true;

    async function refresh() {
      try {
        const classData = await fetchClasses();

        if (!active) return;

        setClasses(classData);
        setRefreshError("");

        if (validStudentId(studentId)) {
          setApplicationsLoading(true);

          const applicationData = await fetchApplications(studentId);

          if (!active) return;

          setApplications(applicationData);
        } else {
          setApplications([]);
        }
      } catch (err) {
        if (active) {
          setRefreshError(errorMessage(err));
        }
      } finally {
        if (active) {
          setInitialLoading(false);
          setApplicationsLoading(false);
        }
      }
    }

    void refresh();

    const interval = window.setInterval(() => {
      void refresh();
    }, 5000);

    return () => {
      active = false;
      window.clearInterval(interval);
    };
  }, [studentId]);

  useEffect(() => {
    const interval = window.setInterval(() => {
      setNow(Date.now());
    }, 1000);

    return () => window.clearInterval(interval);
  }, []);

  async function runAction(
    action: () => Promise<void>,
    successMessage: string
  ) {
    if (busy) return;

    setBusy(true);
    setError("");
    setSuccess("");

    try {
      await action();
      setSuccess(successMessage);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function handleCreateClass(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const seats = Number(totalSeats);

    if (
      !className.trim() ||
      !totalSeats.trim() ||
      !Number.isInteger(seats) ||
      seats <= 0
    ) {
      setError("Please enter a valid class name and seat count.");
      setSuccess("");
      return;
    }

    await runAction(async () => {
      const result = await apiRequest<SchoolClass>("/classes", {
        method: "POST",
        body: JSON.stringify({
          name: className.trim(),
          totalSeats: seats,
        }),
      });

      setClassName("");
      setTotalSeats("");

      const updated = await fetchClasses();
      setClasses(updated);
      setSelectedClassId(result.id);
    }, "Class created successfully.");
  }

  async function handleApply(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!studentIsValid) {
      setError("Enter a valid 17-digit birth registration number.");
      setSuccess("");
      return;
    }

    if (!selectedClass) {
      setError("Please select a class.");
      setSuccess("");
      return;
    }

    if (selectedClass.availableSeats <= 0) {
      setError("No seats are available for this class.");
      setSuccess("");
      return;
    }

    const currentStudentId = studentId;
    const currentClassId = selectedClass.id;

    await runAction(async () => {
      await apiRequest<HoldResponse>(
        `/classes/${encodeURIComponent(currentClassId)}/holds`,
        {
          method: "POST",
          headers: {
            "X-Student-Id": currentStudentId,
          },
        }
      );

      await refreshData(currentStudentId);
    }, "Your seat has been reserved. Complete payment before the hold expires.");
  }

  async function handleConfirm(holdId: string) {
    if (!studentIsValid) return;

    const currentStudentId = studentId;

    await runAction(async () => {
      await apiRequest(
        `/holds/${encodeURIComponent(holdId)}/confirm`,
        {
          method: "POST",
          headers: {
            "X-Student-Id": currentStudentId,
          },
        }
      );

      await refreshData(currentStudentId);
    }, "Admission confirmed successfully.");
  }

  async function handleCancel(holdId: string) {
    if (!studentIsValid) return;

    const currentStudentId = studentId;

    await runAction(async () => {
      await apiRequest(
        `/holds/${encodeURIComponent(holdId)}`,
        {
          method: "DELETE",
          headers: {
            "X-Student-Id": currentStudentId,
          },
        }
      );

      await refreshData(currentStudentId);
    }, "Your application has been cancelled.");
  }

  function getEffectiveStatus(application: Application): string {
    if (
      application.status === "HELD" &&
      getRemainingSeconds(application.expiresAt, now) === 0
    ) {
      return "EXPIRED";
    }

    return application.status;
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="topbar-inner">
          <div className="brand">
            <div className="brand-symbol">SA</div>
            <div className="brand-copy">
              <strong>School Admission</strong>
              <span>Management System</span>
            </div>
          </div>

          <nav className="top-navigation" aria-label="Main navigation">
            <button
              type="button"
              className={`nav-button ${
                view === "student" ? "active" : ""
              }`}
              onClick={() => {
                setView("student");
                setError("");
                setSuccess("");
              }}
            >
              Student Portal
            </button>

            <button
              type="button"
              className={`nav-button ${
                view === "admin" ? "active" : ""
              }`}
              onClick={() => {
                setView("admin");
                setError("");
                setSuccess("");
              }}
            >
              Administration
            </button>
          </nav>
        </div>
      </header>

      <main className="dashboard">
        <div className="page-heading">
          <div>
            <div className="eyebrow">
              {view === "student" ? "STUDENT PORTAL" : "ADMINISTRATION"}
            </div>

            <h1>
              {view === "student"
                ? "Admission Dashboard"
                : "Class Management"}
            </h1>

            <p>
              {view === "student"
                ? "Explore available classes, reserve your seat, and track your admission."
                : "Manage school classes and monitor admission seat availability."}
            </p>
          </div>

          <div className="live-indicator">
            <span className="live-dot" />
            Auto refresh enabled
          </div>
        </div>

        {error && (
          <div className="notice notice-error" role="alert">
            <span className="notice-icon">!</span>
            <span>{error}</span>
            <button type="button" onClick={() => setError("")}>
              Dismiss
            </button>
          </div>
        )}

        {success && (
          <div className="notice notice-success" role="status">
            <span className="notice-icon">✓</span>
            <span>{success}</span>
            <button type="button" onClick={() => setSuccess("")}>
              Dismiss
            </button>
          </div>
        )}

        {refreshError && (
          <div className="notice notice-error" role="alert">
            <span className="notice-icon">!</span>
            <span>Data refresh failed: {refreshError}</span>
            <button
              type="button"
              onClick={() => window.location.reload()}
            >
              Retry
            </button>
          </div>
        )}

        <div className="stats-grid">
          <div className="stat-card">
            <div className="stat-icon stat-icon-blue">CL</div>
            <span className="stat-label">Total Classes</span>
            <strong className="stat-value">{classes.length}</strong>
            <span className="stat-description">Admission classes</span>
          </div>

          <div className="stat-card">
            <div className="stat-icon stat-icon-green">AV</div>
            <span className="stat-label">Available Seats</span>
            <strong className="stat-value">{totalAvailable}</strong>
            <span className="stat-description">Across all classes</span>
          </div>

          <div className="stat-card">
            <div className="stat-icon stat-icon-purple">CF</div>
            <span className="stat-label">Confirmed Seats</span>
            <strong className="stat-value">{totalConfirmed}</strong>
            <span className="stat-description">Successful admissions</span>
          </div>

          <div className="stat-card">
            <div className="stat-icon stat-icon-orange">HD</div>
            <span className="stat-label">
              {view === "student" ? "My Active Holds" : "Currently Held"}
            </span>
            <strong className="stat-value">
              {view === "student"
                ? activeApplications
                : classes.reduce((sum, item) => sum + item.heldSeats, 0)}
            </strong>
            <span className="stat-description">
              {view === "student"
                ? "Your pending applications"
                : "Reserved seats"}
            </span>
          </div>
        </div>

        {view === "student" ? (
          <div className="content-grid">
            <div className="primary-column">
              <section className="panel">
                <div className="panel-heading">
                  <div className="panel-icon">01</div>
                  <div>
                    <h2>Student Information</h2>
                    <p>Enter your birth registration number to continue.</p>
                  </div>
                </div>

                <div className="panel-body">
                  <label htmlFor="studentId">
                    Birth Registration Number
                  </label>

                  <input
                    id="studentId"
                    type="text"
                    inputMode="numeric"
                    autoComplete="off"
                    maxLength={17}
                    value={studentId}
                    disabled={busy}
                    placeholder="Enter your 17-digit number"
                    onChange={(event) => {
                      setStudentId(
                        event.target.value.replace(/\D/g, "")
                      );
                      setApplications([]);
                      setError("");
                      setSuccess("");
                    }}
                  />

                  <p className="field-hint">
                    Your birth registration number is used to identify your applications.
                  </p>

                  {studentId && !studentIsValid && (
                    <p className="validation-text">
                      Please enter exactly 17 digits.
                    </p>
                  )}
                </div>
              </section>

              <section className="panel">
                <div className="panel-heading">
                  <div className="panel-icon">02</div>
                  <div>
                    <h2>Apply for Admission</h2>
                    <p>Choose your class and check available seats.</p>
                  </div>
                </div>

                <div className="panel-body">
                  <form onSubmit={handleApply}>
                    <label htmlFor="classSelect">
                      Select Class / Shift
                    </label>

                    <select
                      id="classSelect"
                      value={selectedClassId}
                      disabled={busy}
                      onChange={(event) =>
                        setSelectedClassId(event.target.value)
                      }
                    >
                      <option value="">Select a class</option>

                      {classes.map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.name} — {item.availableSeats} available
                        </option>
                      ))}
                    </select>

                    {initialLoading && (
                      <div className="empty-state">
                        Loading available classes...
                      </div>
                    )}

                    {!initialLoading && classes.length === 0 && (
                      <div className="empty-state">
                        No admission classes are available yet.
                      </div>
                    )}

                    {selectedClass && (
                      <div className="class-preview">
                        <div className="class-preview-heading">
                          <div>
                            <span className="preview-label">
                              SELECTED CLASS
                            </span>
                            <h3>{selectedClass.name}</h3>
                          </div>

                          <span
                            className={`availability-badge ${
                              selectedClass.availableSeats > 0
                                ? "available"
                                : "unavailable"
                            }`}
                          >
                            {selectedClass.availableSeats > 0
                              ? "Seats Available"
                              : "Fully Booked"}
                          </span>
                        </div>

                        <div className="seat-grid">
                          <div className="seat-item">
                            <span>Total Seats</span>
                            <strong>{selectedClass.totalSeats}</strong>
                          </div>

                          <div className="seat-item">
                            <span>Held Seats</span>
                            <strong>{selectedClass.heldSeats}</strong>
                          </div>

                          <div className="seat-item">
                            <span>Confirmed</span>
                            <strong>{selectedClass.confirmedSeats}</strong>
                          </div>

                          <div className="seat-item seat-available">
                            <span>Available</span>
                            <strong>{selectedClass.availableSeats}</strong>
                          </div>
                        </div>
                      </div>
                    )}

                    <button
                      className="primary-button"
                      type="submit"
                      disabled={
                        busy ||
                        !studentIsValid ||
                        !selectedClass ||
                        selectedClass.availableSeats <= 0
                      }
                    >
                      {busy ? "Processing..." : "Apply for Admission"}
                      <span aria-hidden="true">→</span>
                    </button>
                  </form>
                </div>
              </section>
            </div>

            <div className="secondary-column">
              <section className="panel applications-panel">
                <div className="panel-heading">
                  <div className="panel-icon">03</div>
                  <div>
                    <h2>My Applications</h2>
                    <p>Track and manage your admission applications.</p>
                  </div>
                </div>

                <div className="panel-body">
                  {!studentIsValid ? (
                    <div className="empty-state">
                      <div className="empty-icon">ID</div>
                      <h3>Student ID Required</h3>
                      <p>
                        Enter your 17-digit birth registration number
                        to view your applications.
                      </p>
                    </div>
                  ) : applicationsLoading &&
                    applications.length === 0 ? (
                    <div className="empty-state">
                      Loading your applications...
                    </div>
                  ) : applications.length === 0 ? (
                    <div className="empty-state">
                      <div className="empty-icon">AP</div>
                      <h3>No Applications Found</h3>
                      <p>
                        Select a class and submit an application to get started.
                      </p>
                    </div>
                  ) : (
                    <div className="application-list">
                      {applications.map((application) => {
                        const remaining = getRemainingSeconds(
                          application.expiresAt,
                          now
                        );

                        const status = getEffectiveStatus(application);
                        const canAct =
                          status === "HELD" && remaining > 0;

                        return (
                          <article
                            className="application-card"
                            key={application.holdId}
                          >
                            <div className="application-top">
                              <h3>{application.className}</h3>

                              <span
                                className={`status-pill status-${status.toLowerCase()}`}
                              >
                                {status}
                              </span>
                            </div>

                            <div className="application-info">
                              <div>
                                <span>Application ID</span>
                                <strong className="application-id">
                                  {application.holdId}
                                </strong>
                              </div>

                              <div>
                                <span>Hold Expiry</span>
                                <strong>
                                  {formatDate(application.expiresAt)}
                                </strong>
                              </div>
                            </div>

                            {canAct && (
                              <>
                                <div className="countdown-box">
                                  <div>
                                    <span>Time Remaining</span>
                                    <strong>
                                      Complete payment before expiry
                                    </strong>
                                  </div>

                                  <div className="countdown-number">
                                    {formatCountdown(remaining)}
                                  </div>
                                </div>

                                <div className="application-actions">
                                  <button
                                    type="button"
                                    className="primary-button"
                                    disabled={busy}
                                    onClick={() =>
                                      void handleConfirm(
                                        application.holdId
                                      )
                                    }
                                  >
                                    Pay Admission Fee
                                  </button>

                                  <button
                                    type="button"
                                    className="outline-danger-button"
                                    disabled={busy}
                                    onClick={() =>
                                      void handleCancel(
                                        application.holdId
                                      )
                                    }
                                  >
                                    Cancel
                                  </button>
                                </div>
                              </>
                            )}

                            {status === "CONFIRMED" && (
                              <div className="result-message confirmed-message">
                                Your admission has been confirmed.
                              </div>
                            )}

                            {status === "EXPIRED" && (
                              <div className="result-message expired-message">
                                This reservation has expired.
                              </div>
                            )}

                            {status === "CANCELLED" && (
                              <div className="result-message cancelled-message">
                                This application was cancelled.
                              </div>
                            )}
                          </article>
                        );
                      })}
                    </div>
                  )}
                </div>
              </section>
            </div>
          </div>
        ) : (
          <div className="admin-layout">
            <section className="panel">
              <div className="panel-heading">
                <div className="panel-icon">01</div>
                <div>
                  <h2>Create New Class</h2>
                  <p>Add a class and configure its admission capacity.</p>
                </div>
              </div>

              <div className="panel-body">
                <form onSubmit={handleCreateClass}>
                  <label htmlFor="className">Class / Shift Name</label>

                  <input
                    id="className"
                    type="text"
                    maxLength={200}
                    value={className}
                    disabled={busy}
                    placeholder="Class 6 - Morning Shift"
                    onChange={(event) =>
                      setClassName(event.target.value)
                    }
                  />

                  <label htmlFor="totalSeats">Total Seats</label>

                  <input
                    id="totalSeats"
                    type="number"
                    min="1"
                    step="1"
                    value={totalSeats}
                    disabled={busy}
                    placeholder="120"
                    onChange={(event) =>
                      setTotalSeats(event.target.value)
                    }
                  />

                  <button
                    className="primary-button"
                    type="submit"
                    disabled={busy}
                  >
                    {busy ? "Creating..." : "Create Class"}
                    <span aria-hidden="true">+</span>
                  </button>
                </form>
              </div>
            </section>

            <section className="panel">
              <div className="panel-heading">
                <div className="panel-icon">02</div>
                <div>
                  <h2>Class Overview</h2>
                  <p>Seat distribution across all classes.</p>
                </div>
              </div>

              <div className="panel-body">
                {classes.length === 0 ? (
                  <div className="empty-state">
                    No classes created yet.
                  </div>
                ) : (
                  <div className="admin-class-list">
                    {classes.map((item) => {
                      const occupied =
                        item.heldSeats + item.confirmedSeats;

                      const percentage =
                        item.totalSeats > 0
                          ? Math.min(
                              100,
                              (occupied / item.totalSeats) * 100
                            )
                          : 0;

                      return (
                        <div className="admin-class-item" key={item.id}>
                          <div className="admin-class-top">
                            <strong>{item.name}</strong>
                            <span>
                              {item.availableSeats} available
                            </span>
                          </div>

                          <div className="progress-track">
                            <div
                              className="progress-fill"
                              style={{ width: `${percentage}%` }}
                            />
                          </div>

                          <div className="admin-class-bottom">
                            <span>Total: {item.totalSeats}</span>
                            <span>Held: {item.heldSeats}</span>
                            <span>Confirmed: {item.confirmedSeats}</span>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            </section>
          </div>
        )}

        <footer className="footer">
          <span>School Admission Management System</span>
          <span>Online Admission Portal</span>
        </footer>
      </main>
    </div>
  );
}

export default App;
