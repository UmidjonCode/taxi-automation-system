import { Link } from 'react-router-dom';
import { ArrowRight, Star, Shield, Clock } from 'lucide-react';

export default function LandingPage() {
  return (
    <div className="container">
      <div className="hero-section">
        <span className="tag">Welcome to HotelOS</span>
        <h1>Experience True Luxury</h1>
        <p style={{ maxWidth: '600px', margin: '0 auto 30px', fontSize: '1.2rem' }}>
          Discover a seamless, digital-first hotel experience where your smartphone is your room key and room service is just a tap away.
        </p>
        <Link to="/rooms" className="btn-primary" style={{ display: 'inline-flex', alignItems: 'center', gap: 8, textDecoration: 'none' }}>
          Explore Rooms <ArrowRight size={20} />
        </Link>
      </div>

      <h2 style={{ textAlign: 'center', marginTop: 80 }}>Why Choose Us</h2>
      <div className="grid">
        <div className="glass-card">
          <Shield size={40} color="#3b82f6" style={{ marginBottom: 16 }} />
          <h3>Contactless Entry</h3>
          <p>Get your digital key code instantly upon check-in and head straight to your room. No lines, no wait.</p>
        </div>
        <div className="glass-card">
          <Clock size={40} color="#3b82f6" style={{ marginBottom: 16 }} />
          <h3>24/7 Room Service</h3>
          <p>Order fresh meals and amenities directly to your room at any time of day or night, fully automated.</p>
        </div>
        <div className="glass-card">
          <Star size={40} color="#3b82f6" style={{ marginBottom: 16 }} />
          <h3>Premium Quality</h3>
          <p>Every room is meticulously cleaned by our highly coordinated housekeeping staff just before you arrive.</p>
        </div>
      </div>
    </div>
  );
}
