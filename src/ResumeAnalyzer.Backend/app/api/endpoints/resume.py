from fastapi import APIRouter, UploadFile, File, Form, Depends, HTTPException, status
from typing import Optional, List, Dict, Any
from app.services.parser_service import ParserService
from app.services.nlp_service import NLPService
from app.services.scoring_service import ScoringService
from app.core.security import get_current_user
import uuid
from datetime import datetime

router = APIRouter()
nlp_service = NLPService()
scoring_service = ScoringService(nlp_service)

# In-memory store for demo/stateless fallback of analysis history (in production, Azure SQL is used)
IN_MEMORY_HISTORY: Dict[str, Dict[str, Any]] = {}

@router.post("/upload-resume")
async def upload_resume(
    file: UploadFile = File(...),
    current_user: dict = Depends(get_current_user)
):
    """
    Uploads a resume file, validates it, extracts metadata, and returns the result.
    """
    content_type = file.content_type
    filename = file.filename
    
    if not filename.lower().endswith(('.pdf', '.docx')):
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Invalid file format. Only PDF and DOCX are allowed."
        )
        
    try:
        file_bytes = await file.read()
        text = ParserService.extract_text(file_bytes, filename)
        file_size = len(file_bytes)
        
        return {
            "id": str(uuid.uuid4()),
            "filename": filename,
            "content_type": content_type,
            "size_bytes": file_size,
            "text_length": len(text)
        }
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error processing file: {str(e)}"
        )

@router.post("/extract-text")
async def extract_text(
    file: UploadFile = File(...),
    current_user: dict = Depends(get_current_user)
):
    """
    Extracts and returns plain text from a resume file.
    """
    filename = file.filename
    if not filename.lower().endswith(('.pdf', '.docx')):
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Invalid file format. Only PDF and DOCX are allowed."
        )
        
    try:
        file_bytes = await file.read()
        text = ParserService.extract_text(file_bytes, filename)
        return {"filename": filename, "text": text}
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error extracting text: {str(e)}"
        )

@router.post("/analyze-resume")
async def analyze_resume(
    file: Optional[UploadFile] = File(None),
    text: Optional[str] = Form(None),
    current_user: dict = Depends(get_current_user)
):
    """
    Analyzes resume text (or uploaded file) to extract skills, experience, education, etc.
    """
    if not file and not text:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Either file or text must be provided."
        )
        
    try:
        resume_text = text
        filename = "pasted_text.txt"
        if file:
            file_bytes = await file.read()
            filename = file.filename
            resume_text = ParserService.extract_text(file_bytes, filename)
            
        analysis = nlp_service.analyze_resume_text(resume_text)
        return {
            "filename": filename,
            "text_length": len(resume_text),
            "analysis": analysis
        }
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error analyzing resume: {str(e)}"
        )

@router.post("/compare-job-description")
async def compare_job_description(
    file: Optional[UploadFile] = File(None),
    resume_text: Optional[str] = Form(None),
    job_description: str = Form(...),
    current_user: dict = Depends(get_current_user)
):
    """
    Compares a resume (file or text) against a Job Description to generate ATS score and suggestions.
    """
    if not file and not resume_text:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Either file or resume_text must be provided."
        )
        
    try:
        text = resume_text
        filename = "pasted_text.txt"
        if file:
            file_bytes = await file.read()
            filename = file.filename
            text = ParserService.extract_text(file_bytes, filename)
            
        # 1. NLP Parse
        analysis = nlp_service.analyze_resume_text(text)
        
        # 2. Score Match
        score_result = scoring_service.calculate_score(text, analysis, job_description)
        
        # Save to memory (simulates DB storage for REST compliance)
        analysis_id = str(uuid.uuid4())
        record = {
            "id": analysis_id,
            "user_id": current_user["user_id"],
            "filename": filename,
            "date": datetime.utcnow().isoformat(),
            "ats_score": score_result["ats_score"],
            "keyword_match_percentage": score_result["keyword_match_percentage"],
            "missing_skills": score_result["missing_skills"],
            "recommendations": score_result["recommendations"],
            "formatting_issues": score_result["formatting_issues"],
            "grammar_suggestions": score_result["grammar_suggestions"],
            "resume_summary": score_result["resume_summary"],
            "extracted_data": score_result["extracted_data"]
        }
        IN_MEMORY_HISTORY[analysis_id] = record
        
        return record
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error comparing job description: {str(e)}"
        )

@router.get("/analysis-history")
async def get_analysis_history(current_user: dict = Depends(get_current_user)):
    """
    Retrieves user's analysis history records.
    """
    user_id = current_user["user_id"]
    user_records = [
        rec for rec in IN_MEMORY_HISTORY.values() 
        if rec["user_id"] == user_id
    ]
    return sorted(user_records, key=lambda x: x["date"], reverse=True)

@router.delete("/analysis/{id}")
async def delete_analysis(id: str, current_user: dict = Depends(get_current_user)):
    """
    Deletes a specific analysis record from the history.
    """
    if id not in IN_MEMORY_HISTORY:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Analysis record not found."
        )
        
    record = IN_MEMORY_HISTORY[id]
    if record["user_id"] != current_user["user_id"] and current_user.get("role") != "Admin":
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="You do not have permission to delete this analysis."
        )
        
    del IN_MEMORY_HISTORY[id]
    return {"message": "Analysis record deleted successfully."}
