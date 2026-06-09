from fastapi import FastAPI, Request, status
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from app.core.config import settings
from app.api.endpoints import resume
import time

app = FastAPI(
    title=settings.PROJECT_NAME,
    openapi_url=f"{settings.API_V1_STR}/openapi.json",
    docs_url="/docs",
    redoc_url="/redoc"
)

# CORS configuration
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Adjust for production frontend domain
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# In-memory simple IP-based rate limiting dictionary
# key: IP address, value: [timestamp1, timestamp2, ...]
RATE_LIMIT_STORE = {}

@app.middleware("http")
async def rate_limiting_middleware(request: Request, call_next):
    """
    Very lightweight rate limiting middleware.
    In commercial SaaS deployment, use Redis (e.g. slowapi / fastapi-limiter).
    """
    # Exclude docs and health checks from rate limiting if desired
    if request.url.path in ["/health", "/docs", f"{settings.API_V1_STR}/openapi.json"]:
        return await call_next(request)
        
    client_ip = request.client.host
    now = time.time()
    
    # Clean up old entries
    if client_ip in RATE_LIMIT_STORE:
        RATE_LIMIT_STORE[client_ip] = [
            t for t in RATE_LIMIT_STORE[client_ip] 
            if now - t < settings.RATE_LIMIT_WINDOW_SECONDS
        ]
    else:
        RATE_LIMIT_STORE[client_ip] = []
        
    if len(RATE_LIMIT_STORE[client_ip]) >= settings.RATE_LIMIT_REQUESTS:
        return JSONResponse(
            status_code=status.HTTP_429_TOO_MANY_REQUESTS,
            content={"detail": "Too many requests. Please try again later."}
        )
        
    RATE_LIMIT_STORE[client_ip].append(now)
    return await call_next(request)

# Include APIs
app.include_router(resume.router, prefix=settings.API_V1_STR, tags=["resume"])

@app.get("/health", status_code=status.HTTP_200_OK, tags=["monitoring"])
def health_check():
    """
    Service health check endpoint.
    """
    return {
        "status": "healthy",
        "timestamp": time.time(),
        "service": settings.PROJECT_NAME
    }

@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    """
    Global exception logger and handler.
    """
    return JSONResponse(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        content={"detail": f"Internal Server Error: {str(exc)}"}
    )
