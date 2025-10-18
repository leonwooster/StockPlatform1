// StockSense Pro - Main JavaScript File
// Advanced Stock Analysis Platform

// Global variables
let currentStock = 'AAPL';
let currentTimeframe = '1M';
let watchlist = ['AAPL', 'MSFT', 'GOOGL', 'TSLA', 'NVDA'];
let stockData = {};
let charts = {};

// Sample stock data for demonstration
const sampleStocks = {
    'AAPL': { name: 'Apple Inc.', price: 175.43, change: 2.15, changePercent: 1.24, volume: '45.2M' },
    'MSFT': { name: 'Microsoft Corp.', price: 378.85, change: -1.23, changePercent: -0.32, volume: '23.1M' },
    'GOOGL': { name: 'Alphabet Inc.', price: 142.56, change: 3.78, changePercent: 2.72, volume: '18.7M' },
    'TSLA': { name: 'Tesla Inc.', price: 248.50, change: -8.90, changePercent: -3.45, volume: '89.3M' },
    'NVDA': { name: 'NVIDIA Corp.', price: 495.23, change: 12.45, changePercent: 2.58, volume: '34.6M' },
    'AMZN': { name: 'Amazon.com Inc.', price: 145.78, change: 1.89, changePercent: 1.31, volume: '28.4M' },
    'META': { name: 'Meta Platforms', price: 325.67, change: -2.34, changePercent: -0.71, volume: '15.2M' },
    'NFLX': { name: 'Netflix Inc.', price: 412.34, change: 8.90, changePercent: 2.20, volume: '7.8M' }
};

// Initialize the application
function initializeApp() {
    initializeTypedText();
    loadMarketData();
    initializeWatchlist();
    initializeMainChart();
    initializeSparklines();
    loadTopMovers();
    loadTradingSignals();
    loadNewsTicker();
    setupSearchFunctionality();
    
    // Add entrance animations
    animatePageLoad();
}

// Initialize typed text animation
function initializeTypedText() {
    new Typed('#typed-text', {
        strings: [
            'Advanced Stock Analysis',
            'Real-Time Market Data',
            'AI-Powered Trading Signals',
            'Professional Trading Tools'
        ],
        typeSpeed: 50,
        backSpeed: 30,
        backDelay: 2000,
        loop: true,
        showCursor: true,
        cursorChar: '|'
    });
}

// Load market data for major indices
function loadMarketData() {
    // Simulate real-time market data updates
    const indices = [
        { id: 'sp500', base: 4567.89, volatility: 0.02 },
        { id: 'nasdaq', base: 14234.56, volatility: 0.03 },
        { id: 'dow', base: 35678.90, volatility: 0.025 }
    ];
    
    indices.forEach(index => {
        updateIndexData(index);
    });
}

// Update individual index data
function updateIndexData(index) {
    const change = (Math.random() - 0.5) * index.volatility * index.base;
    const newPrice = index.base + change;
    const changePercent = (change / index.base) * 100;
    
    const priceElement = document.getElementById(`${index.id}-price`);
    const changeElement = document.getElementById(`${index.id}-change`);
    
    if (priceElement && changeElement) {
        priceElement.textContent = newPrice.toLocaleString('en-US', { 
            minimumFractionDigits: 2, 
            maximumFractionDigits: 2 
        });
        
        const changeText = `${change >= 0 ? '+' : ''}${change.toFixed(2)} (${change >= 0 ? '+' : ''}${changePercent.toFixed(2)}%)`;
        changeElement.textContent = changeText;
        
        // Update color based on change
        changeElement.className = change >= 0 ? 'text-sm price-positive' : 'text-sm price-negative';
        
        // Add pulse animation for significant changes
        if (Math.abs(changePercent) > 0.5) {
            priceElement.classList.add('pulse-green');
            setTimeout(() => priceElement.classList.remove('pulse-green'), 2000);
        }
    }
}

// Initialize watchlist
function initializeWatchlist() {
    const watchlistElement = document.getElementById('watchlist');
    if (!watchlistElement) return;
    
    watchlistElement.innerHTML = '';
    
    watchlist.forEach(symbol => {
        const stock = sampleStocks[symbol];
        if (!stock) return;
        
        const item = document.createElement('div');
        item.className = 'watchlist-item p-3 rounded-lg cursor-pointer';
        item.onclick = () => selectStock(symbol);
        
        item.innerHTML = `
            <div class="flex items-center justify-between">
                <div>
                    <div class="font-semibold text-white">${symbol}</div>
                    <div class="text-xs text-gray-400">${stock.name}</div>
                </div>
                <div class="text-right">
                    <div class="mono-font font-semibold text-white">$${stock.price.toFixed(2)}</div>
                    <div class="text-xs ${stock.change >= 0 ? 'price-positive' : 'price-negative'}">
                        ${stock.change >= 0 ? '+' : ''}${stock.change.toFixed(2)}
                    </div>
                </div>
            </div>
        `;
        
        watchlistElement.appendChild(item);
    });
}

// Select a stock from watchlist
function selectStock(symbol) {
    currentStock = symbol;
    const stock = sampleStocks[symbol];
    
    // Update header information
    document.getElementById('selectedStock').textContent = `${symbol} - ${stock.name}`;
    document.getElementById('currentPrice').textContent = `$${stock.price.toFixed(2)}`;
    
    const changeElement = document.getElementById('priceChange');
    const changeText = `${stock.change >= 0 ? '+' : ''}${stock.change.toFixed(2)} (${stock.change >= 0 ? '+' : ''}${stock.changePercent.toFixed(2)}%)`;
    changeElement.textContent = changeText;
    changeElement.className = stock.change >= 0 ? 'text-lg price-positive' : 'text-lg price-negative';
    
    // Update main chart
    updateMainChart(symbol);
    
    // Animate selection
    anime({
        targets: '#selectedStock',
        scale: [1, 1.05, 1],
        duration: 300,
        easing: 'easeInOutQuad'
    });
}

// Initialize main chart
function initializeMainChart() {
    const chartElement = document.getElementById('mainChart');
    if (!chartElement) return;
    
    charts.mainChart = echarts.init(chartElement);
    updateMainChart(currentStock);
}

// Update main chart with new stock data
function updateMainChart(symbol) {
    if (!charts.mainChart) return;
    
    // Generate sample candlestick data
    const data = generateCandlestickData(30);
    
    const option = {
        backgroundColor: 'transparent',
        grid: {
            left: '3%',
            right: '4%',
            bottom: '3%',
            containLabel: true
        },
        xAxis: {
            type: 'category',
            data: data.dates,
            axisLine: { lineStyle: { color: '#4a5568' } },
            axisLabel: { color: '#a0aec0' }
        },
        yAxis: {
            type: 'value',
            scale: true,
            axisLine: { lineStyle: { color: '#4a5568' } },
            axisLabel: { color: '#a0aec0' },
            splitLine: { lineStyle: { color: '#2d3748' } }
        },
        series: [{
            type: 'candlestick',
            data: data.values,
            itemStyle: {
                color: '#38a169',
                color0: '#e53e3e',
                borderColor: '#38a169',
                borderColor0: '#e53e3e'
            }
        }],
        tooltip: {
            trigger: 'axis',
            backgroundColor: 'rgba(45, 55, 72, 0.9)',
            borderColor: '#4a5568',
            textStyle: { color: '#e2e8f0' }
        }
    };
    
    charts.mainChart.setOption(option);
}

// Generate sample candlestick data
function generateCandlestickData(days) {
    const dates = [];
    const values = [];
    const basePrice = sampleStocks[currentStock]?.price || 100;
    
    for (let i = 0; i < days; i++) {
        const date = new Date();
        date.setDate(date.getDate() - (days - i));
        dates.push(date.toLocaleDateString());
        
        const open = basePrice + (Math.random() - 0.5) * 10;
        const close = open + (Math.random() - 0.5) * 5;
        const high = Math.max(open, close) + Math.random() * 3;
        const low = Math.min(open, close) - Math.random() * 3;
        
        values.push([open.toFixed(2), close.toFixed(2), low.toFixed(2), high.toFixed(2)]);
    }
    
    return { dates, values };
}

// Initialize sparkline charts for indices
function initializeSparklines() {
    const sparklineIds = ['sp500-sparkline', 'nasdaq-sparkline', 'dow-sparkline'];
    
    sparklineIds.forEach(id => {
        const element = document.getElementById(id);
        if (!element) return;
        
        const chart = echarts.init(element);
        const data = Array.from({ length: 20 }, () => Math.random() * 100);
        
        const option = {
            backgroundColor: 'transparent',
            grid: { left: 0, right: 0, top: 0, bottom: 0 },
            xAxis: { type: 'category', show: false, data: Array(20).fill('') },
            yAxis: { type: 'value', show: false },
            series: [{
                type: 'line',
                data: data,
                smooth: true,
                symbol: 'none',
                lineStyle: { color: '#319795', width: 2 },
                areaStyle: { 
                    color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                        { offset: 0, color: 'rgba(49, 151, 149, 0.3)' },
                        { offset: 1, color: 'rgba(49, 151, 149, 0.05)' }
                    ])
                }
            }]
        };
        
        chart.setOption(option);
    });
}

// Load top movers
function loadTopMovers() {
    const moversElement = document.getElementById('topMovers');
    if (!moversElement) return;
    
    const movers = Object.entries(sampleStocks)
        .sort((a, b) => Math.abs(b[1].changePercent) - Math.abs(a[1].changePercent))
        .slice(0, 8);
    
    moversElement.innerHTML = '';
    
    movers.forEach(([symbol, stock]) => {
        const item = document.createElement('div');
        item.className = 'grid grid-cols-3 gap-4 py-2 border-b border-gray-700 last:border-b-0';
        
        item.innerHTML = `
            <div class="font-medium text-white">${symbol}</div>
            <div class="mono-font text-right text-white">$${stock.price.toFixed(2)}</div>
            <div class="text-right ${stock.change >= 0 ? 'price-positive' : 'price-negative'}">
                ${stock.change >= 0 ? '+' : ''}${stock.changePercent.toFixed(2)}%
            </div>
        `;
        
        moversElement.appendChild(item);
    });
}

// Load trading signals
function loadTradingSignals() {
    const signalsElement = document.getElementById('tradingSignals');
    if (!signalsElement) return;
    
    const signals = [
        { symbol: 'AAPL', signal: 'BUY', strength: 85, target: 180.50, stopLoss: 168.20 },
        { symbol: 'MSFT', signal: 'HOLD', strength: 45, target: 385.00, stopLoss: 365.00 },
        { symbol: 'NVDA', signal: 'BUY', strength: 92, target: 520.00, stopLoss: 475.00 },
        { symbol: 'TSLA', signal: 'SELL', strength: 78, target: 235.00, stopLoss: 265.00 }
    ];
    
    signalsElement.innerHTML = '';
    
    signals.forEach(signal => {
        const item = document.createElement('div');
        item.className = 'p-4 rounded-lg bg-gray-800/50 border border-gray-700';
        
        const signalColor = signal.signal === 'BUY' ? 'text-green-400' : 
                           signal.signal === 'SELL' ? 'text-red-400' : 'text-yellow-400';
        
        item.innerHTML = `
            <div class="flex items-center justify-between mb-2">
                <span class="font-semibold text-white">${signal.symbol}</span>
                <span class="${signalColor} font-bold">${signal.signal}</span>
            </div>
            <div class="text-sm text-gray-400 mb-2">Strength: ${signal.strength}%</div>
            <div class="flex justify-between text-xs text-gray-500">
                <span>Target: $${signal.target}</span>
                <span>Stop: $${signal.stopLoss}</span>
            </div>
            <div class="w-full bg-gray-700 rounded-full h-2 mt-2">
                <div class="bg-teal-500 h-2 rounded-full" style="width: ${signal.strength}%"></div>
            </div>
        `;
        
        signalsElement.appendChild(item);
    });
}

// Load news ticker
function loadNewsTicker() {
    const newsElement = document.getElementById('newsTicker');
    if (!newsElement) return;
    
    const newsItems = [
        'Fed signals potential rate cuts in 2024 amid cooling inflation',
        'Tech stocks rally as AI adoption accelerates across industries',
        'Apple reports strong Q4 earnings, iPhone sales exceed expectations',
        'Tesla announces new Gigafactory expansion in Texas',
        'Microsoft Azure growth drives cloud sector optimism',
        'NVIDIA CEO sees continued AI chip demand through 2025'
    ];
    
    newsElement.innerHTML = '';
    
    newsItems.forEach((item, index) => {
        const newsItem = document.createElement('div');
        newsItem.className = 'flex-shrink-0 px-4 py-2 bg-gray-800/50 rounded-lg';
        newsItem.innerHTML = `
            <span class="text-gray-300 text-sm">${item}</span>
            <span class="text-teal-400 text-xs ml-2">${Math.floor(Math.random() * 60) + 1}m ago</span>
        `;
        newsElement.appendChild(newsItem);
        
        // Add entrance animation
        anime({
            targets: newsItem,
            translateX: [-50, 0],
            opacity: [0, 1],
            delay: index * 200,
            duration: 500,
            easing: 'easeOutQuad'
        });
    });
}

// Setup search functionality
function setupSearchFunctionality() {
    const searchInput = document.getElementById('stockSearch');
    const searchResults = document.getElementById('searchResults');
    
    if (!searchInput || !searchResults) return;
    
    searchInput.addEventListener('input', function(e) {
        const query = e.target.value.toUpperCase();
        
        if (query.length < 1) {
            searchResults.classList.add('hidden');
            return;
        }
        
        const matches = Object.keys(sampleStocks)
            .filter(symbol => symbol.includes(query))
            .slice(0, 5);
        
        if (matches.length > 0) {
            searchResults.innerHTML = '';
            matches.forEach(symbol => {
                const stock = sampleStocks[symbol];
                const item = document.createElement('div');
                item.className = 'px-4 py-2 hover:bg-gray-700 cursor-pointer';
                item.innerHTML = `
                    <div class="font-medium text-white">${symbol}</div>
                    <div class="text-sm text-gray-400">${stock.name}</div>
                `;
                item.onclick = () => {
                    selectStock(symbol);
                    searchInput.value = '';
                    searchResults.classList.add('hidden');
                };
                searchResults.appendChild(item);
            });
            searchResults.classList.remove('hidden');
        } else {
            searchResults.classList.add('hidden');
        }
    });
    
    // Hide search results when clicking outside
    document.addEventListener('click', function(e) {
        if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
            searchResults.classList.add('hidden');
        }
    });
}

// Change chart timeframe
function changeTimeframe(timeframe) {
    currentTimeframe = timeframe;
    
    // Update button states
    document.querySelectorAll('[onclick^="changeTimeframe"]').forEach(btn => {
        btn.className = 'px-3 py-1 bg-gray-700 text-white rounded text-sm hover:bg-gray-600';
    });
    
    event.target.className = 'px-3 py-1 bg-teal-600 text-white rounded text-sm';
    
    // Update chart with new timeframe
    updateMainChart(currentStock);
}

// Add stock to watchlist
function addToWatchlist() {
    const symbol = prompt('Enter stock symbol:');
    if (symbol && sampleStocks[symbol.toUpperCase()] && !watchlist.includes(symbol.toUpperCase())) {
        watchlist.push(symbol.toUpperCase());
        initializeWatchlist();
        
        // Show success message
        showNotification(`${symbol.toUpperCase()} added to watchlist`, 'success');
    } else if (watchlist.includes(symbol.toUpperCase())) {
        showNotification('Stock already in watchlist', 'warning');
    } else if (symbol) {
        showNotification('Stock not found', 'error');
    }
}

// Show notification
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `fixed top-20 right-6 px-6 py-3 rounded-lg text-white z-50 ${
        type === 'success' ? 'bg-green-600' :
        type === 'warning' ? 'bg-yellow-600' :
        type === 'error' ? 'bg-red-600' : 'bg-blue-600'
    }`;
    notification.textContent = message;
    
    document.body.appendChild(notification);
    
    anime({
        targets: notification,
        translateX: [300, 0],
        opacity: [0, 1],
        duration: 300,
        easing: 'easeOutQuad'
    });
    
    setTimeout(() => {
        anime({
            targets: notification,
            translateX: [0, 300],
            opacity: [1, 0],
            duration: 300,
            easing: 'easeInQuad',
            complete: () => notification.remove()
        });
    }, 3000);
}

// Animate page load
function animatePageLoad() {
    anime.timeline()
        .add({
            targets: '.metric-card',
            translateY: [50, 0],
            opacity: [0, 1],
            delay: anime.stagger(100),
            duration: 600,
            easing: 'easeOutQuad'
        })
        .add({
            targets: '.glass-card',
            scale: [0.95, 1],
            opacity: [0, 1],
            delay: anime.stagger(50),
            duration: 500,
            easing: 'easeOutQuad'
        }, '-=400');
}

// Start real-time updates
function startRealTimeUpdates() {
    // Update market data every 5 seconds
    setInterval(() => {
        loadMarketData();
        updateWatchlistPrices();
    }, 5000);
    
    // Update news ticker every 30 seconds
    setInterval(() => {
        loadNewsTicker();
    }, 30000);
}

// Update watchlist prices
function updateWatchlistPrices() {
    watchlist.forEach(symbol => {
        const stock = sampleStocks[symbol];
        if (stock) {
            // Simulate small price changes
            const change = (Math.random() - 0.5) * 0.5;
            stock.price += change;
            stock.change += change;
            stock.changePercent = (stock.change / (stock.price - stock.change)) * 100;
        }
    });
    
    initializeWatchlist();
    
    // Update current stock if it's in view
    if (currentStock && sampleStocks[currentStock]) {
        const stock = sampleStocks[currentStock];
        document.getElementById('currentPrice').textContent = `$${stock.price.toFixed(2)}`;
        
        const changeElement = document.getElementById('priceChange');
        const changeText = `${stock.change >= 0 ? '+' : ''}${stock.change.toFixed(2)} (${stock.change >= 0 ? '+' : ''}${stock.changePercent.toFixed(2)}%)`;
        changeElement.textContent = changeText;
        changeElement.className = stock.change >= 0 ? 'text-lg price-positive' : 'text-lg price-negative';
    }
}

// Handle window resize for charts
window.addEventListener('resize', function() {
    Object.values(charts).forEach(chart => {
        if (chart && chart.resize) {
            chart.resize();
        }
    });
});

// Export functions for global access
window.selectStock = selectStock;
window.changeTimeframe = changeTimeframe;
window.addToWatchlist = addToWatchlist;