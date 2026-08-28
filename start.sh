#!/bin/bash

# Impo - Startup Script
# Tüm servisleri tek komutla başlatır

set -e

# Renkler
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}"
echo "╔═══════════════════════════════════════╗"
echo "║         🚀 IMPO STARTER v3.0         ║"
echo "║   Tüm Servisleri Başlatıyor...       ║"
echo "╚═══════════════════════════════════════╝"
echo -e "${NC}"
echo ""

# Cleanup function
cleanup() {
    echo ""
    echo -e "${YELLOW}Servisler durduruluyor...${NC}"
    pkill -P $$ 2>/dev/null || true
    echo -e "${GREEN}Tüm servisler durduruldu.${NC}"
    exit 0
}

trap cleanup SIGINT SIGTERM

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET SDK bulunamadı!${NC}"
    echo "Lütfen .NET 10 SDK'yı yükleyin: https://dotnet.microsoft.com/download"
    exit 1
fi

echo -e "${GREEN}✓${NC} .NET SDK bulundu: $(dotnet --version)"
echo ""

# Build projesi
echo -e "${BLUE}📦 Proje build ediliyor...${NC}"
if dotnet build --nologo --verbosity quiet; then
    echo -e "${GREEN}✓${NC} Build başarılı!"
else
    echo -e "${RED}❌ Build başarısız!${NC}"
    exit 1
fi
echo ""

# Port kontrolü
check_port() {
    if lsof -Pi :$1 -sTCP:LISTEN -t >/dev/null 2>&1 ; then
        echo -e "${YELLOW}⚠${NC}  Port $1 kullanımda. Servis durdurulsun mu? (y/n)"
        read -r response
        if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
            lsof -ti:$1 | xargs kill -9 2>/dev/null || true
            sleep 1
        else
            echo -e "${RED}❌ Port $1 kullanımda. Çıkılıyor.${NC}"
            exit 1
        fi
    fi
}

echo -e "${BLUE}🔍 Portlar kontrol ediliyor...${NC}"
check_port 5185
check_port 5057
check_port 5209
check_port 5047
echo -e "${GREEN}✓${NC} Tüm portlar müsait!"
echo ""

# Servis başlatma fonksiyonu
start_service() {
    local name=$1
    local project=$2
    local port=$3
    local icon=$4
    
    echo -e "${BLUE}${icon} ${name} başlatılıyor (Port: ${port})...${NC}"
    dotnet run --project "$project" --launch-profile http > "logs/${name}.log" 2>&1 &
    local pid=$!
    echo "$pid" > "logs/${name}.pid"
    
    # Servisin başlamasını bekle
    for i in {1..30}; do
        if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1 ; then
            echo -e "${GREEN}✓${NC} ${name} başarıyla başlatıldı!"
            return 0
        fi
        sleep 1
    done
    
    echo -e "${RED}❌ ${name} başlatılamadı!${NC}"
    cat "logs/${name}.log"
    return 1
}

# Log klasörü oluştur
mkdir -p logs

echo -e "${BLUE}🚀 Servisler başlatılıyor...${NC}"
echo ""

# Servisleri başlat
start_service "Spectator" "src/Spectator/Spectator.Api/Spectator.Api.csproj" "5185" "🏟️"
start_service "LocalCrm-API" "src/LocalCrm/LocalCrm.Api/LocalCrm.Api.csproj" "5057" "👥"
start_service "LocalCrm-App" "src/LocalCrm/LocalCrm.App/LocalCrm.App.csproj" "5209" "📱"
start_service "PromptGateway" "src/PromptGateway/PromptGateway.Api/PromptGateway.Api.csproj" "5047" "🤖"

echo ""
echo -e "${GREEN}"
echo "╔═══════════════════════════════════════╗"
echo "║     ✅ TÜM SERVİSLER BAŞLATILDI!     ║"
echo "╚═══════════════════════════════════════╝"
echo -e "${NC}"
echo ""

# URL'leri göster
echo -e "${BLUE}📍 Erişim URL'leri:${NC}"
echo ""
echo -e "  🏟️  ${GREEN}Spectator:${NC}      http://localhost:5185"
echo -e "  👥  ${GREEN}LocalCrm API:${NC}   http://localhost:5057"
echo -e "  📱  ${GREEN}LocalCrm App:${NC}   http://localhost:5209"
echo -e "  🤖  ${GREEN}PromptGateway:${NC}  http://localhost:5047"
echo ""
echo -e "  🎯  ${GREEN}Dashboard:${NC}      file://$(pwd)/dashboard.html"
echo ""
echo -e "${YELLOW}📊 Loglar: logs/ klasöründe${NC}"
echo -e "${YELLOW}🛑 Durdurmak için: Ctrl+C${NC}"
echo ""

# Dashboard'u aç
if command -v open &> /dev/null; then
    echo -e "${BLUE}🌐 Dashboard tarayıcıda açılıyor...${NC}"
    open dashboard.html
elif command -v xdg-open &> /dev/null; then
    xdg-open dashboard.html
fi

# Sonsuz döngü - Ctrl+C ile çıkılana kadar
echo -e "${GREEN}✨ Servisler çalışıyor... (Loglar canlı olarak izleniyor)${NC}"
echo ""

tail -f logs/*.log 2>/dev/null &
wait
