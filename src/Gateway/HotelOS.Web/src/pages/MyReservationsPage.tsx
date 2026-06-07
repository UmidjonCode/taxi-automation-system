import { useState, useEffect } from 'react';
import { getAuth, authFetch } from '../App';

export default function MyReservationsPage() {
  const [bookings, setBookings] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const { isLoggedIn, fullName } = getAuth();

  useEffect(() => {
    if (!isLoggedIn) return;
    (async () => {
      try {
        const res = await authFetch('http://localhost:5001/api/bookings/my');
        if (res.ok) {
          const data = await res.json();
          setBookings(Array.isArray(data) ? data : []);
        } else if (res.status === 401) {
          localStorage.clear();
          window.location.href = '/login';
        }
      } catch (err) {
        console.error(err);
      }
      setLoading(false);
    })();
  }, []);

  if (!isLoggedIn) {
    return (
      <div className="container" style={{ textAlign: 'center', marginTop: 80 }}>
        <div className="glass-card" style={{ maxWidth: 400, margin: '0 auto' }}>
          <h2>Please Login</h2>
          <p style={{ marginBottom: 20 }}>You need to login to view your reservations.</p>
          <a href="/login" className="btn-primary">Go to Login</a>
        </div>
      </div>
    );
  }

  const statusColor = (s: string) => {
    switch(s) {
      case 'Confirmed': return '#22c55e';
      case 'Pending': return '#f59e0b';
      case 'CheckedIn': return '#3b82f6';
      case 'CheckedOut': return '#6b7280';
      case 'Cancelled': return '#ef4444';
      default: return '#9ca3af';
    }
  };

  return (
    <div className="container">
      <h2 style={{ marginBottom: 8 }}>My Reservations</h2>
      <p style={{ color: 'var(--text-secondary)', marginBottom: 30 }}>Welcome back, {fullName}!</p>

      {loading ? (
        <p>Loading your reservations...</p>
      ) : bookings.length === 0 ? (
        <div className="glass-card" style={{ textAlign: 'center' }}>
          <p>You don't have any reservations yet.</p>
          <a href="/rooms" className="btn-primary" style={{ display: 'inline-block', marginTop: 16 }}>Browse Rooms</a>
        </div>
      ) : (
        <div className="grid">
          {bookings.map(b => (
            <div key={b.bookingId} className="glass-card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
                <h3>Room {b.roomNumber}</h3>
                <span style={{
                  background: statusColor(b.status),
                  color: 'white',
                  padding: '4px 12px',
                  borderRadius: 12,
                  fontSize: '0.8rem',
                  fontWeight: 'bold'
                }}>{b.status}</span>
              </div>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
                Rate: ${b.nightlyRate}/night<br/>
                Advance Paid: ${b.advancePayment}
              </p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
