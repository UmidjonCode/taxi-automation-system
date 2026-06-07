import { useState, useEffect } from 'react';

export default function MyReservationsPage() {
  const [reservations, setReservations] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const email = localStorage.getItem('guestEmail');

  useEffect(() => {
    if (!email) return;
    
    fetch(`http://localhost:5001/api/bookings/my?email=${encodeURIComponent(email)}`)
      .then(res => res.json())
      .then(data => {
        setReservations(data);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, [email]);

  if (!email) {
    return (
      <div className="container" style={{ textAlign: 'center', marginTop: 100 }}>
        <h2>Access Denied</h2>
        <p>Please log in to view your reservations.</p>
      </div>
    );
  }

  return (
    <div className="container">
      <h2>My Reservations</h2>
      <p style={{ marginBottom: 30 }}>Welcome back, {email}</p>
      
      {loading ? (
        <p>Loading your reservations...</p>
      ) : reservations.length === 0 ? (
        <div className="glass-card" style={{ textAlign: 'center', padding: 60 }}>
          <h3>No Reservations Found</h3>
          <p>We couldn't find any upcoming stays for this email address.</p>
        </div>
      ) : (
        <div className="grid">
          {reservations.map((res: any) => (
            <div key={res.bookingId} className="glass-card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                <span className="tag">{res.status}</span>
                <span style={{ fontWeight: 'bold', color: '#3b82f6' }}>Room {res.roomNumber}</span>
              </div>
              <p><strong>Booking ID:</strong> {res.bookingId.split('-')[0]}...</p>
              <p><strong>Nightly Rate:</strong> ${res.nightlyRate}</p>
              <p><strong>Advance Paid:</strong> ${res.advancePayment}</p>
              
              {res.status === 'Confirmed' && (
                <button 
                  className="btn-primary" 
                  style={{ width: '100%', marginTop: 20 }}
                  onClick={() => alert(`Check-in code ready at front desk.`)}
                >
                  View Digital Key
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
