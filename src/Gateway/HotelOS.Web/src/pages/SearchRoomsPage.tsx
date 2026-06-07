import { useState } from 'react';

export default function SearchRoomsPage() {
  const [checkIn, setCheckIn] = useState('');
  const [checkOut, setCheckOut] = useState('');
  const [rooms, setRooms] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);

  // Booking Modal State
  const [selectedRoom, setSelectedRoom] = useState<any>(null);
  const [password, setPassword] = useState('');
  const [email, setEmail] = useState('');

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!checkIn || !checkOut) return;
    
    setLoading(true);
    setSearched(true);
    try {
      const res = await fetch(`http://localhost:5001/api/rooms/search?checkIn=${checkIn}T14:00:00Z&checkOut=${checkOut}T11:00:00Z`);
      const data = await res.json();
      setRooms(data);
    } catch (err) {
      console.error(err);
      alert('Failed to connect to the Reception API.');
    }
    setLoading(false);
  };

  const handleBook = async (e: React.FormEvent) => {
    e.preventDefault();

    // First attempt to login the user
    try {
      const authRes = await fetch('http://localhost:5001/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });

      if (!authRes.ok) {
        alert('Authentication failed. Please check your email and password, or register an account first.');
        return;
      }
      
      const authData = await authRes.json();

      const payload = {
        guestId: authData.guestId, // Use GuestId instead of Guest details
        style: selectedRoom.style === 'Standard' ? 0 : selectedRoom.style === 'Deluxe' ? 1 : 2,
        checkIn: `${checkIn}T14:00:00Z`,
        checkOut: `${checkOut}T11:00:00Z`,
        preferredFloor: selectedRoom.floor,
        advancePayment: selectedRoom.nightlyRate
      };

      const res = await fetch('http://localhost:5001/api/bookings', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      
      if (res.ok) {
        alert('Booking confirmed!');
        localStorage.setItem('guestEmail', email);
        window.location.href = '/my-reservations';
      } else {
        const err = await res.json();
        alert('Failed to book: ' + (err.error || 'Unknown error'));
      }
    } catch (err) {
      console.error(err);
      alert('Network error occurred.');
    }
  };

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
              <div key={room.id} className="glass-card" style={{ opacity: room.isAvailable ? 1 : 0.6, position: 'relative' }}>
                <span className="tag">{room.style}</span>
                {!room.isAvailable && (
                  <span style={{ position: 'absolute', top: 24, right: 24, background: '#ef4444', color: 'white', padding: '4px 12px', borderRadius: '12px', fontSize: '0.8rem', fontWeight: 'bold' }}>
                    Reserved
                  </span>
                )}
                <h3>Room {room.roomNumber}</h3>
                <p>Floor: {room.floor} | Zone: {room.proximityZone}</p>
                <h2 style={{ color: '#fff', marginTop: 16, marginBottom: 16 }}>${room.nightlyRate}<span style={{fontSize: '1rem', color: 'var(--text-secondary)'}}>/night</span></h2>
                <button 
                  className="btn-primary" 
                  style={{ width: '100%', background: room.isAvailable ? 'var(--accent-color)' : '#4b5563', cursor: room.isAvailable ? 'pointer' : 'not-allowed' }} 
                  onClick={() => { if (room.isAvailable) setSelectedRoom(room); }}
                  disabled={!room.isAvailable}
                >
                  {room.isAvailable ? 'Book Now' : 'Currently Unavailable'}
                </button>
              </div>
            ))}
          </div>
        </>
      )}

      {selectedRoom && (
        <div style={{
          position: 'fixed', top: 0, left: 0, width: '100%', height: '100%', 
          background: 'rgba(0,0,0,0.8)', backdropFilter: 'blur(4px)',
          display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
          <div className="glass-card" style={{ width: '100%', maxWidth: '500px' }}>
            <h2>Confirm Booking</h2>
            <p style={{ marginBottom: 20 }}>Room {selectedRoom.roomNumber} • ${selectedRoom.nightlyRate}/night</p>
            <p style={{ color: 'var(--text-secondary)', marginBottom: 20, fontSize: '0.9rem' }}>Please enter your login details to confirm this booking. If you don't have an account, please Register first.</p>
            <form onSubmit={handleBook}>
              <input className="input-field" type="email" placeholder="Email Address" value={email} onChange={e => setEmail(e.target.value)} required style={{ marginBottom: 12 }} />
              <input className="input-field" type="password" placeholder="Password" value={password} onChange={e => setPassword(e.target.value)} required style={{ marginBottom: 24 }} />
              
              <div style={{ display: 'flex', gap: 12 }}>
                <button type="button" className="btn-primary" style={{ background: 'transparent', border: '1px solid var(--glass-border)' }} onClick={() => setSelectedRoom(null)}>Cancel</button>
                <button type="submit" className="btn-primary" style={{ flex: 1 }}>Pay ${selectedRoom.nightlyRate} & Confirm</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
