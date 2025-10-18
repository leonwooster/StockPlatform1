# Stock Analysis Application - Design Style Guide

## Design Philosophy

### Visual Language
**Modern Financial Professional**: Clean, sophisticated interface that conveys trust, precision, and institutional-grade quality. The design draws inspiration from Bloomberg Terminal aesthetics while maintaining the accessibility and elegance of modern web applications.

### Color Palette
**Primary Colors**:
- **Deep Navy**: #1a2332 (Primary background, headers)
- **Steel Blue**: #2d3748 (Secondary backgrounds, panels)
- **Accent Teal**: #319795 (Success states, buy signals)
- **Warning Amber**: #d69e2e (Caution states, hold signals)

**Data Visualization Colors** (All with saturation <50%):
- **Positive Green**: #38a169 (Gains, bullish indicators)
- **Negative Red**: #e53e3e (Losses, bearish indicators)
- **Neutral Gray**: #718096 (Neutral data, grid lines)

### Typography
**Primary Font**: "Inter" - Modern, highly legible sans-serif for data-heavy interfaces
**Secondary Font**: "JetBrains Mono" - Monospace for numerical data, code, and terminal-style outputs
**Display Font**: "Poppins" - Bold, confident headings that convey authority

### Layout Principles
- **Grid-based Design**: 12-column responsive grid system
- **Data Density**: High information density without clutter
- **Visual Hierarchy**: Clear distinction between primary, secondary, and tertiary information
- **Breathing Room**: Adequate whitespace to prevent cognitive overload

## Visual Effects & Styling

### Background Treatment
**Liquid Metal Displacement Effect**: Subtle animated background using shader-park for a sophisticated, premium feel. The effect creates gentle, flowing metallic textures that suggest precision and high-tech financial analysis.

### Interactive Elements
**Micro-Interactions**:
- **Button Hover**: Subtle 3D lift with shadow expansion
- **Chart Hover**: Smooth data point highlighting with tooltip animations
- **Loading States**: Elegant skeleton screens with shimmer effects
- **Data Updates**: Smooth number counting animations for price changes

### Chart Styling
**Candlestick Charts**: 
- Clean, minimal design with proper spacing
- Color-coded bullish/bearish candles
- Smooth zoom and pan interactions
- Subtle grid lines for readability

**Technical Indicators**:
- Thin, precise lines for moving averages
- Transparent filled areas for volume indicators
- Consistent color coding across all charts

### Animation Library Usage
**Anime.js**: 
- Smooth transitions between dashboard states
- Staggered animations for data loading
- Chart drawing animations

**ECharts.js**:
- Primary charting library for all financial visualizations
- Custom themes matching our color palette
- Interactive tooltips and crosshairs

**Splitting.js**:
- Text reveal animations for key metrics
- Staggered letter animations for headings

### Header Effects
**Aurora Gradient Flow**: Subtle animated gradient background for the main header area, creating a sense of movement and dynamism without being distracting. The gradient flows between deep navy and steel blue tones.

### Data Visualization Principles
- **Consistent Color Usage**: Same colors represent same concepts across all charts
- **Accessible Contrast**: All text maintains 4.5:1 contrast ratio minimum
- **Progressive Disclosure**: Complex data revealed through interaction
- **Context Preservation**: Always show relevant context and reference points

### Component Styling
**Cards**: 
- Subtle shadows with rounded corners
- Clean borders with hover states
- Consistent padding and spacing

**Forms**:
- Modern input styling with focus states
- Clear validation feedback
- Consistent button styling

**Navigation**:
- Clean, minimal design
- Active state indicators
- Smooth transitions between sections

### Mobile Considerations
- **Responsive Typography**: Font sizes scale appropriately
- **Touch-Friendly**: Minimum 44px touch targets
- **Simplified Layouts**: Essential information prioritized on smaller screens
- **Swipe Gestures**: Natural navigation for chart exploration

### Accessibility Features
- **High Contrast Mode**: Alternative color scheme for better visibility
- **Keyboard Navigation**: Full keyboard accessibility for all interactions
- **Screen Reader Support**: Proper ARIA labels and semantic HTML
- **Reduced Motion**: Respect user preferences for reduced animations

This design system creates a professional, trustworthy environment that serious investors and traders expect while maintaining the modern, responsive experience that makes financial analysis accessible and efficient.