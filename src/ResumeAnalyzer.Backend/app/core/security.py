from datetime import datetime, timedelta, timezone
from typing import Optional, Union, Any
from jose import jwt, JWTError
from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from app.core.config import settings

security_scheme = HTTPBearer(auto_error=False)

def verify_token(token: str) -> Optional[dict]:
    """
    Decodes and verifies a JWT token. Returns the payload dict if valid, else None.
    """
    try:
        payload = jwt.decode(
            token,
            settings.JWT_SECRET,
            algorithms=[settings.JWT_ALGORITHM]
        )
        return payload
    except JWTError:
        return None

def get_current_user(credentials: Optional[HTTPAuthorizationCredentials] = Depends(security_scheme)) -> dict:
    """
    Dependency that extracts user information from JWT token in the Authorization header.
    """
    if not credentials:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Authorization header missing or invalid",
            headers={"WWW-Authenticate": "Bearer"},
        )
    
    token = credentials.credentials
    payload = verify_token(token)
    
    if payload is None:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Could not validate credentials",
            headers={"WWW-Authenticate": "Bearer"},
        )
        
    # Standard JWT claims (sub, exp, etc.)
    # ASP.NET Identity uses specific claim names, e.g., 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
    # We will search for 'sub' or common identity claims
    user_id = payload.get("sub") or payload.get("nameid") or payload.get("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
    email = payload.get("email") or payload.get("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
    role = payload.get("role") or payload.get("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
    
    if not user_id:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Token missing user identification",
            headers={"WWW-Authenticate": "Bearer"},
        )
        
    return {
        "user_id": user_id,
        "email": email,
        "role": role,
        "claims": payload
    }
