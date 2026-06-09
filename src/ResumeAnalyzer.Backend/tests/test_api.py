import pytest
from fastapi.testclient import TestClient
from app.main import app
from app.services.parser_service import ParserService
from app.services.nlp_service import NLPService
from app.services.scoring_service import ScoringService

client = TestClient(app)

def test_health_check():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json()["status"] == "healthy"

def test_nlp_extraction():
    nlp = NLPService()
    test_text = """
    John Doe
    Email: john.doe@example.com
    Phone: (123) 456-7890
    
    Education:
    Bachelor of Science in Computer Science - Tech University, 2018 - 2022
    
    Skills: Python, FastAPI, Docker, PostgreSQL, Communication, Agile.
    
    Experience:
    Software Engineer at TechCorp (2022 - Present)
    Built REST APIs using Python, Django, and PostgreSQL.
    Managed deployments using Docker and GitHub Actions.
    """
    
    analysis = nlp.analyze_resume_text(test_text)
    assert "john.doe@example.com" in analysis["emails"]
    assert "(123) 456-7890" in analysis["phones"]
    assert "python" in analysis["skills"]
    assert "fastapi" in analysis["skills"]
    assert "docker" in analysis["skills"]
    assert len(analysis["education"]) > 0
    assert analysis["education"][0]["degree"] == "Bachelor's Degree"
    assert "Computer CS" in analysis["education"][0]["major"] or "Computer Science" in analysis["education"][0]["raw_text"]
    assert analysis["experience_years"] >= 2.0

def test_scoring_engine():
    nlp = NLPService()
    scoring = ScoringService(nlp)
    
    resume_text = "Experienced software engineer specializing in Python, FastAPI, Docker, and MySQL."
    resume_analysis = nlp.analyze_resume_text(resume_text)
    
    jd_text = "Looking for a Python Developer with experience in FastAPI, Kubernetes, and PostgreSQL."
    
    score_result = scoring.calculate_score(resume_text, resume_analysis, jd_text)
    
    assert "ats_score" in score_result
    assert "keyword_match_percentage" in score_result
    assert "missing_skills" in score_result
    assert "kubernetes" in score_result["missing_skills"]
    assert "postgresql" in score_result["missing_skills"]
