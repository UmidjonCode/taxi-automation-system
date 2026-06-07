import { useState, useEffect, useRef } from 'react';
import { getAuth, authFetch } from '../App';

export default function SearchRoomsPage() {
  const [checkIn, setCheckIn] = useState('');
  const [checkOut, setCheckOut] = useState('');
  const [rooms, setRooms] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);

  // Hold/Payment modal state
  const [activeHold, setActiveHold] = useState<any>(null);
  const [timeLeft, setTimeLeft] = useState(0);
  const timerRef = useRef<any>(null);

  const { isLoggedIn, guestId } = getAuth();

  // Countdown timer for active hold
  useEffect(() => {
    if (activeHold) {
      const expiresAt = new Date(activeHold.expiresAt).getTime();
      const tick = () => {
        const remaining = Math.max(0, Math.floor((expiresAt - Date.now()) / 1000));
        setTimeLeft(remaining);
        if (remaining <= 0) {
          clearInterval(timerRef.current);
          setActiveHold(null);
          alert('Your hold has expired. The room is now available to others.');
        }
      };
      tick();
      timerRef.current = setInterval(tick, 1000);
      return () => clearInterval(timerRef.current);
    }
  }, [activeHold]);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!checkIn || !checkOut) return;
    setLoading(true);
    setSearched(true);
    try {
      const res = await fetch(`http://localhost:5001/api/rooms/search?checkIn=${checkIn}T14:00:00Z&checkOut=${checkOut}T11:00:00Z`);
      const data = await res.json();
      setRooms(Array.isArray(data) ? data : []);
    } catch (err) {
      alert('Failed to connect to the Reception API.');
    }
    setLoading(false);
  };

  const handleHold = async (room: any) => {
    if (!isLoggedIn) {
      alert('Please login first to book a room.');
      window.location.href = '/login';
      return;
    }

    try {
      const res = await authFetch('http://localhost:5001/api/bookings/hold', {
        method: 'POST',
        body: JSON.stringify({
          roomId: room.id,
          guestId: guestId,
          checkIn: `${checkIn}T14:00:00Z`,
          checkOut: `${checkOut}T11:00:00Z`
        })
      });

      if (res.ok) {
        const hold = await res.json();
        setActiveHold({ ...hold, room });
      } else {
        const err = await res.json();
        alert(err.error || 'Failed to hold room.');
      }
    } catch (err) {
      alert('Network error.');
    }
  };

  const handleConfirmPayment = async () => {
    if (!activeHold) return;
    try {
      const res = await authFetch(`http://localhost:5001/api/bookings/hold/${activeHold.id}/confirm`, {
        method: 'POST',
        body: JSON.stringify({ advancePayment: activeHold.room.nightlyRate })
      });

      if (res.ok) {
        clearInterval(timerRef.current);
        setActiveHold(null);
        alert('🎉 Booking confirmed! Redirecting to your reservations...');
        window.location.href = '/my-reservations';
      } else {
        const err = await res.json();
        alert(err.error || 'Failed to confirm booking.');
      }
    } catch (err) {
      alert('Network error.');
    }
  };

  const handleCancelHold = async () => {
    if (!activeHold) return;
    try {
      await authFetch(`http://localhost:5001/api/bookings/hold/${activeHold.id}`, { method: 'DELETE' });
    } catch {}
    clearInterval(timerRef.current);
    setActiveHold(null);
  };

  const formatTime = (s: number) => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;

  return (
    <div className="container">
      <div className="glass-card" style={{ marginBottom: 40 }}>
        <h2>Find Your Perfect Room</h2>
        <form onSubmit={handleSearch} style={{ display: 'flex', gap: 16, alignItems: 'flex-end', flexWrap: 'wrap' }}>
          <div style={{ flex: 1, minWidth: '200px' }}>
            <label style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Check-in Date</label>
            <input type="date" className="input-field" value={checkIn} onChange={e => setCheckIn(e.target.value)} required />
          </div>
          <div style={{ flex: 1, minWidth: '200px' }}>
            <label style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Check-out Date</label>
            <input type="date" className="input-field" value={checkOut} onChange={e => setCheckOut(e.target.value)} required />
          </div>
          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? 'Searching...' : 'Search Availability'}
          </button>
        </form>
      </div>

      {searched && (
        <>
          <h3 style={{ marginBottom: 20 }}>All Rooms for these dates</h3>
          <div className="grid">
            {rooms.map(room => (
              <div key={room.id} className="glass-card" style={{ opacity: room.isAvailable ? 1 : 0.5, position: 'relative', filter: room.isAvailable ? 'none' : 'grayscale(40%)' }}>
                <span className="tag">{room.style}</span>
                {!room.isAvailable && (
                  <span style={{ position: 'absolute', top: 24, right: 24, background: 'linear-gradient(135deg, #ef4444, #dc2626)', color: 'white', padding: '4px 14px', borderRadius: '12px', fontSize: '0.8rem', fontWeight: 'bold', boxShadow: '0 2px 8px rgba(239,68,68,0.3)' }}>
                    Reserved
                  </span>
                )}
                <h3>Room {room.roomNumber}</h3>
                <p>Floor: {room.floor} | Zone: {room.proximityZone}</p>
                <h2 style={{ color: '#fff', marginTop: 16, marginBottom: 16 }}>${room.nightlyRate}<span style={{fontSize: '1rem', color: 'var(--text-secondary)'}}>/night</span></h2>
                <button 
                  className="btn-primary" 
                  style={{ width: '100%', background: room.isAvailable ? '' : '#4b5563', cursor: room.isAvailable ? 'pointer' : 'not-allowed' }} 
                  onClick={() => room.isAvailable && handleHold(room)}
                  disabled={!room.isAvailable}
                >
                  {room.isAvailable ? '🔒 Hold & Book' : 'Currently Unavailable'}
                </button>
              </div>
            ))}
          </div>
        </>
      )}

      {/* Hold/Payment Modal with Countdown */}
      {activeHold && (
        <div style={{
          position: 'fixed', top: 0, left: 0, width: '100%', height: '100%', 
          background: 'rgba(0,0,0,0.85)', backdropFilter: 'blur(6px)',
          display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
          <div className="glass-card" style={{ width: '100%', maxWidth: '480px', textAlign: 'center' }}>
            {/* Timer */}
            <div style={{
              fontSize: '2.5rem', fontWeight: 800, fontFamily: 'monospace',
              color: timeLeft <= 60 ? '#ef4444' : '#3b82f6',
              marginBottom: 8,
              animation: timeLeft <= 30 ? 'pulse 1s infinite' : 'none'
            }}>
              {formatTime(timeLeft)}
            </div>
            <p style={{ color: 'var(--text-secondary)', marginBottom: 24, fontSize: '0.9rem' }}>
              Room held for you. Complete payment before the timer runs out!
            </p>

            <div style={{ background: 'rgba(255,255,255,0.05)', borderRadius: 12, padding: 20, marginBottom: 24, textAlign: 'left' }}>
              <h3 style={{ marginBottom: 8 }}>Room {activeHold.room.roomNumber}</h3>
              <p>Style: {activeHold.room.style} | Floor: {activeHold.room.floor}</p>
              <p style={{ marginTop: 8 }}>Check-in: <strong>{checkIn}</strong></p>
              <p>Check-out: <strong>{checkOut}</strong></p>
              <h2 style={{ marginTop: 16, color: '#3b82f6' }}>${activeHold.room.nightlyRate} <span style={{ fontSize: '1rem', color: 'var(--text-secondary)' }}>advance</span></h2>
            </div>

            <div style={{ display: 'flex', gap: 12 }}>
              <button className="btn-primary" onClick={handleCancelHold} style={{ background: 'transparent', border: '1px solid var(--glass-border)', flex: 1 }}>
                Cancel
              </button>
              <button className="btn-primary" onClick={handleConfirmPayment} style={{ flex: 2 }}>
                💳 Confirm & Pay ${activeHold.room.nightlyRate}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
