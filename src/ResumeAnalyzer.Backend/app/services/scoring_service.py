import re
from typing import Dict, List, Any
from app.services.nlp_service import NLPService

class ScoringService:
    def __init__(self, nlp_service: NLPService):
        self.nlp = nlp_service

    def analyze_job_description(self, jd_text: str) -> Dict[str, Any]:
        """
        Parses the Job Description to extract required skills, experience, and key terms.
        """
        jd_skills = self.nlp.extract_skills(jd_text)
        
        # Estimate required experience
        req_exp = 0.0
        exp_match = re.search(r'(\d+)\s*\+?\s*years?(?:\s+of)?\s+experience', jd_text, re.IGNORECASE)
        if exp_match:
            try:
                req_exp = float(exp_match.group(1))
            except ValueError:
                pass
                
        # Key terms (nouns, adverbs of relevance - simplified to high-frequency non-stopwords)
        words = re.findall(r'\b[a-zA-Z]{3,20}\b', jd_text.lower())
        stopwords_set = self.nlp.stop_words
        keywords = [w for w in words if w not in stopwords_set and len(w) > 3]
        
        # Frequency distribution
        freq = {}
        for w in keywords:
            freq[w] = freq.get(w, 0) + 1
        sorted_keywords = sorted(freq.items(), key=lambda x: x[1], reverse=True)
        top_keywords = [k[0] for k in sorted_keywords[:15]]
        
        return {
            "skills": jd_skills,
            "required_experience": req_exp,
            "top_keywords": top_keywords
        }

    def calculate_score(self, resume_text: str, resume_analysis: Dict[str, Any], jd_text: str) -> Dict[str, Any]:
        """
        Compares resume analysis against a Job Description and scores the resume.
        """
        jd_analysis = self.analyze_job_description(jd_text)
        
        # 1. Skill Match (Weight: 40%)
        jd_skills = set(jd_analysis["skills"])
        resume_skills = set(resume_analysis["skills"])
        
        matching_skills = list(jd_skills.intersection(resume_skills))
        missing_skills = list(jd_skills.difference(resume_skills))
        
        skill_score = 100.0
        if jd_skills:
            skill_score = (len(matching_skills) / len(jd_skills)) * 100.0
            
        # 2. Keyword Match (Weight: 20%)
        jd_keywords = set(jd_analysis["top_keywords"])
        resume_words = set(re.findall(r'\b[a-zA-Z]{3,20}\b', resume_text.lower()))
        matched_keywords = list(jd_keywords.intersection(resume_words))
        
        keyword_score = 100.0
        if jd_keywords:
            keyword_score = (len(matched_keywords) / len(jd_keywords)) * 100.0
            
        # 3. Experience Match (Weight: 20%)
        req_exp = jd_analysis["required_experience"]
        have_exp = resume_analysis["experience_years"]
        
        if req_exp > 0:
            if have_exp >= req_exp:
                experience_score = 100.0
            else:
                experience_score = (have_exp / req_exp) * 100.0
        else:
            # General score if no experience is specified in JD
            experience_score = min(100.0, (have_exp / 2.0) * 100.0) if have_exp < 2.0 else 100.0

        # 4. Education & Formatting Match (Weight: 20%)
        formatting_score = 100.0
        formatting_issues = []
        
        # Check email & phone
        if not resume_analysis["emails"]:
            formatting_score -= 15
            formatting_issues.append("Missing contact email address.")
        if not resume_analysis["phones"]:
            formatting_score -= 10
            formatting_issues.append("Missing contact phone number.")
            
        # Check education
        if not resume_analysis["education"]:
            formatting_score -= 25
            formatting_issues.append("No education details (degree or major) identified.")
            
        # Check word count / page density
        word_count = len(resume_text.split())
        if word_count < 150:
            formatting_score -= 30
            formatting_issues.append("Resume contains very little text. Ensure it is not an scanned image or missing sections.")
        elif word_count > 1500:
            formatting_score -= 10
            formatting_issues.append("Resume is too long (over 1500 words). Try to condense to 1-2 pages.")
            
        formatting_score = max(0.0, formatting_score)
        
        # Grammar suggestions (dummy generator for this NLP parser - checks simple double spaces or common errors)
        grammar_suggestions = []
        double_spaces = len(re.findall(r' {2,}', resume_text))
        if double_spaces > 3:
            grammar_suggestions.append(f"Found {double_spaces} double spaces. Remove them to clean formatting.")
            
        # Calculate overall weighted ATS score
        ats_score = (skill_score * 0.40) + (keyword_score * 0.20) + (experience_score * 0.20) + (formatting_score * 0.20)
        ats_score = int(round(ats_score))
        
        # Ensure it stays within bounds
        ats_score = max(0, min(100, ats_score))
        
        # Generate actionable recommendations
        recommendations = []
        if missing_skills:
            # Highlight top missing skills
            top_missing = missing_skills[:5]
            recommendations.append(f"Incorporate missing core skills: {', '.join(top_missing)}.")
        if req_exp > have_exp:
            recommendations.append(f"This position requires about {int(req_exp)} years of experience, while your resume indicates around {have_exp} years. Highlight transferable skills or relevant projects.")
        if not resume_analysis["projects"]:
            recommendations.append("Add a 'Projects' section to showcase your hands-on applications of skills.")
        if not resume_analysis["certifications"] and "certif" in jd_text.lower():
            recommendations.append("Add relevant professional certifications (e.g. AWS, Scrum Master) mentioned in the job description.")
        if formatting_issues:
            recommendations.append(f"Resolve formatting issues: {formatting_issues[0]}")
            
        if not recommendations:
            recommendations.append("Your resume aligns well with this job. Consider minor edits to match specific phrasing.")
            
        # Resume summary generation based on experience and top skills
        skills_summary = ", ".join(resume_analysis["skills"][:5])
        edu_summary = resume_analysis["education"][0]["degree"] if resume_analysis["education"] else "Professional"
        resume_summary = f"{edu_summary} with approximately {have_exp} years of experience. Key competencies include: {skills_summary}."
        
        return {
            "ats_score": ats_score,
            "keyword_match_percentage": int(round(keyword_score)),
            "missing_skills": missing_skills,
            "recommendations": recommendations,
            "formatting_issues": formatting_issues,
            "grammar_suggestions": grammar_suggestions,
            "resume_summary": resume_summary,
            "extracted_data": {
                "skills": resume_analysis["skills"],
                "education": resume_analysis["education"],
                "experience_years": resume_analysis["experience_years"],
                "certifications": resume_analysis["certifications"],
                "projects": resume_analysis["projects"],
                "emails": resume_analysis["emails"],
                "phones": resume_analysis["phones"]
            }
        }
