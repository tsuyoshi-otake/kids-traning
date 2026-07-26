using System.Globalization;
using System.Text.Json;
using KidsTraining.App.Domain.Learning;

namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildProgressionScript()
    {
        var catalog = CurriculumPolicy.AllUnits.Select(unit => new
        {
            id = unit.Id,
            subjectId = unit.SubjectId,
            topicId = unit.TopicId,
            label = unit.Label,
            grade = unit.Grade,
            order = unit.Order,
            prerequisites = unit.Prerequisites,
            generatorKey = unit.GeneratorKey,
            assessmentMode = unit.AssessmentMode,
            sourceReference = unit.SourceReference,
            questions = unit.Questions.Select(question => new
            {
                stage = question.Stage,
                prompt = question.Prompt,
                answer = question.Answer,
                distractors = question.Distractors,
                explanation = question.Explanation,
                activityPrompt = question.ActivityPrompt
            })
        });
        var catalogJson = JsonSerializer.Serialize(catalog);
        var lanesJson = JsonSerializer.Serialize(CurriculumPolicy.CurriculumLanes);
        var reviewIntervals = string.Join(',', Enumerable.Range(0, ReviewSchedule.MaximumStep + 1)
            .Select(step => ((long)ReviewSchedule.IntervalAt(step).TotalMilliseconds).ToString(CultureInfo.InvariantCulture)));
        var requiredStageAttempts = SkillEvidence.RequiredStageAttempts.ToString(CultureInfo.InvariantCulture);
        var requiredStageIndependent = SkillEvidence.RequiredStageIndependentCorrect.ToString(CultureInfo.InvariantCulture);
        var requiredRetentionConfirmations = SkillEvidence.RequiredRetentionConfirmations.ToString(CultureInfo.InvariantCulture);

        return $$$"""
skillAverage(p){const values=Object.values((p&&p.mastery)||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));return values.length?values.reduce((a,b)=>a+b,0)/values.length:0.05;}
  pickStage(stage,buckets,reviewRate=.25){const top=Math.max(1,Math.min(5,Number(stage)||1));let current=top-1;while(current>0&&!(buckets[current]&&buckets[current].length))current--;const active=(buckets[current]||[]).map(fn=>({fn:fn,difficulty:current+1})),previous=[];for(let i=0;i<current;i++)for(const fn of (buckets[i]||[]))previous.push({fn:fn,difficulty:i+1});const pool=previous.length&&Math.random()<reviewRate?previous:(active.length?active:previous);if(!pool.length)throw new Error('No questions configured for stage '+top);const picked=pool[this.rand(0,pool.length-1)],q=picked.fn();q.difficulty=picked.difficulty;return q;}
  curriculumCatalog(){return {{{catalogJson}}};}
  curriculumLaneIds(){return {{{lanesJson}}};}
  curriculumUnit(k){return this.curriculumCatalog().find(unit=>unit.id===k)||null;}
  curriculumUnitsForTopic(k){return this.curriculumCatalog().filter(unit=>unit.topicId===k);}
  effectiveGrade(p){return Math.max(1,Math.min(9,Number(p&&p.grade)||1));}
  schoolGradeName(value){const grade=Math.max(1,Math.min(9,Number(value)||1));return grade<=6?'小学'+grade+'年':'中学'+(grade-6)+'年';}
  gradeLabel(p){return this.schoolGradeName(this.effectiveGrade(p))+'生';}
  learningStage(p){const level=this.skillLevel(p),stars=Number(p.stars)||0;if(stars<15&&level<=1)return 1;if(stars<45||level<=2)return 2;if(stars<90||level<=3)return 3;if(stars<150||level<=4)return 4;return 5;}
  blankUnitStat(){return{attempts:0,independent:0,assisted:0,revealed:0,errors:0,confidence:.05,reviewStep:0,retentionStep:0,lastAttemptAt:null,nextReviewAt:null,retentionStartedAt:null,masteredAt:null,level:1,stageAttempts:0,stageIndependent:0};}
  completedUnitStat(now=Date.now()){return{...this.blankUnitStat(),confidence:.99,retentionStep:{{{requiredRetentionConfirmations}}},retentionStartedAt:now,masteredAt:now,level:5};}
  normalizeUnitStat(old,mastery=.05){const base=this.blankUnitStat(),m=this.clamp(Number(mastery)||.05,.05,.99),mastered=Number(old&&old.masteredAt)||null,retentionStarted=Number(old&&old.retentionStartedAt)||(mastered?mastered:null),derived=retentionStarted?5:Math.max(1,Math.min(5,Math.floor(m*5)+1));return{...base,attempts:Number(old&&old.attempts)||0,independent:Number(old&&old.independent)||0,assisted:Number(old&&old.assisted)||0,revealed:Number(old&&old.revealed)||0,errors:Number(old&&old.errors)||0,confidence:this.clamp(Number(old&&old.confidence)||m,.05,.99),reviewStep:Math.max(0,Math.min(3,Number(old&&old.reviewStep)||0)),retentionStep:Math.max(0,Math.min({{{requiredRetentionConfirmations}}},Number(old&&old.retentionStep)||(mastered?{{{requiredRetentionConfirmations}}}:0))),lastAttemptAt:Number(old&&old.lastAttemptAt)||null,nextReviewAt:Number(old&&old.nextReviewAt)||null,retentionStartedAt:retentionStarted,masteredAt:mastered,level:Math.max(1,Math.min(5,Number(old&&old.level)||derived)),stageAttempts:Number(old&&old.stageAttempts)||0,stageIndependent:Number(old&&old.stageIndependent)||0};}
  ensureLearningProfile(p){if(!p)return p;const catalog=this.curriculumCatalog(),wasV5=Number(p.learningSchema)>=5,legacyGrade=this.effectiveGrade(p),oldTopicStats=p.skillStats||{},oldMastery=p.mastery||{};p.mastery=p.mastery||{};p.skillStats=p.skillStats||{};p.cleared=p.cleared||{};p.unitStats=p.unitStats||{};if(!wasV5&&!p.legacyTopicStats)p.legacyTopicStats=JSON.parse(JSON.stringify(oldTopicStats));const migrated={};for(const unit of catalog){if(p.unitStats[unit.id]){migrated[unit.id]=this.normalizeUnitStat(p.unitStats[unit.id],p.mastery[unit.id]||p.mastery[unit.topicId]);continue;}if(wasV5){migrated[unit.id]=this.blankUnitStat();continue;}const old=oldTopicStats[unit.topicId]||{},evidence=(Number(old.attempts)||0)>0||Number(old.lastAttemptAt)>0,wasComplete=!!(p.cleared[unit.topicId]||old.masteredAt||old.retentionStartedAt);const eligible=this.curriculumUnitsForTopic(unit.topicId).filter(candidate=>candidate.grade<=legacyGrade),target=eligible.find(candidate=>candidate.grade===legacyGrade)||eligible[eligible.length-1],beforeTarget=target&&unit.order<target.order;if(wasComplete&&unit.grade<=legacyGrade)migrated[unit.id]=this.completedUnitStat(Number(old.masteredAt)||Number(old.lastAttemptAt)||Date.now());else if(evidence&&beforeTarget)migrated[unit.id]=this.completedUnitStat(Number(old.lastAttemptAt)||Date.now());else if(evidence&&target&&unit.id===target.id)migrated[unit.id]=this.normalizeUnitStat(old,oldMastery[unit.topicId]);else migrated[unit.id]=this.blankUnitStat();}p.unitStats=migrated;for(const unit of catalog)p.mastery[unit.id]=migrated[unit.id].confidence;p.learningSchema=5;return p;}
  migrateProfiles(profiles){return (Array.isArray(profiles)?profiles:[]).map(p=>this.ensureLearningProfile({...p,mastery:{...(p.mastery||{})},skillStats:{...(p.skillStats||{})},unitStats:{...(p.unitStats||{})},cleared:{...(p.cleared||{})}}));}
  resolveUnitId(p,k){this.ensureLearningProfile(p);if(this.curriculumUnit(k))return k;const active=this.curriculumUnit(p&&p._activeUnitId);if(active&&active.topicId===k)return active.id;const units=this.curriculumUnitsForTopic(k),configured=this.state&&this.state.settings&&this.state.settings.topics,enabled=units.filter(unit=>!configured||configured[unit.topicId]!==false),pool=enabled.length?enabled:units;if(!pool.length)return k;return (pool.find(unit=>{const s=p.unitStats[unit.id];return !(s&&(s.retentionStartedAt||s.masteredAt));})||pool[pool.length-1]).id;}
  topicStat(p,k){this.ensureLearningProfile(p);const id=this.resolveUnitId(p,k);return p.unitStats[id]||(p.unitStats[id]=this.blankUnitStat());}
  topicStage(p,k){const s=this.topicStat(p,k),saved=Number(s&&s.level);if(Number.isFinite(saved))return Math.max(1,Math.min(5,saved));return Math.max(1,Math.min(5,Math.floor((Number(s&&s.confidence)||.05)*5)+1));}
  topicLearningStage(p,k){return this.topicStat(p,k).retentionStartedAt?6:this.topicStage(p,k);}
  reviewStage(p,k){const s=this.topicStat(p,k),stage=this.topicStage(p,k);if(s.retentionStartedAt)return 5;return this.topicDue(p,k)&&stage>1?Math.max(1,stage-1):stage;}
  profileAtStage(p,k,stage){const source=this.ensureLearningProfile({...p,mastery:{...((p&&p.mastery)||{})},skillStats:{...((p&&p.skillStats)||{})},unitStats:{...((p&&p.unitStats)||{})},cleared:{...((p&&p.cleared)||{})}}),id=this.resolveUnitId(source,k),level=Math.max(1,Math.min(5,stage)),levels=[.05,.25,.45,.65,.85],unit=this.curriculumUnit(id),stats={...(source.unitStats[id]||this.blankUnitStat()),level:level,confidence:levels[level-1]};source._activeUnitId=id;source.unitStats[id]=stats;source.mastery[id]=stats.confidence;if(unit){source.grade=unit.grade;source.mastery[unit.topicId]=stats.confidence;source.skillStats[unit.topicId]=stats;}return source;}
  topicMastered(p,k){return !!this.topicStat(p,k).masteredAt;}
  topicComplete(p,k){const s=this.topicStat(p,k);return !!(s.retentionStartedAt||s.masteredAt);}
  topicDue(p,k,now=Date.now()){const n=this.topicStat(p,k).nextReviewAt;return !!n&&n<=now;}
  topicReady(p,k,now=Date.now()){const s=this.topicStat(p,k);return !!s.masteredAt&&s.retentionStep>={{{requiredRetentionConfirmations}}}&&!this.topicDue(p,k,now);}
  stageEvidenceRequired(level){return level<=2?{attempts:4,independent:3}:level<=4?{attempts:5,independent:4}:{attempts:{{{requiredStageAttempts}}},independent:{{{requiredStageIndependent}}}};}
  stageEvidenceReady(s){const req=this.stageEvidenceRequired(s.level),accuracy=s.stageAttempts?s.stageIndependent/s.stageAttempts:0;return s.stageAttempts>=req.attempts&&s.stageIndependent>=req.independent&&accuracy>=req.independent/req.attempts;}
  beginRetention(s,now,intervals){s.retentionStartedAt=now;s.retentionStep=0;s.reviewStep=0;s.stageAttempts=0;s.stageIndependent=0;s.nextReviewAt=now+intervals[0];}
  markCleared(p,k,now=Date.now()){const id=this.resolveUnitId(p,k),s=this.topicStat(p,id);if(s.retentionStep>={{{requiredRetentionConfirmations}}}&&!s.masteredAt){s.masteredAt=now;(p.cleared=p.cleared||{})[id]=true;}}
  recordEvidence(p,q,outcome,points){const id=q.unitId||this.resolveUnitId(p,q.topic),unit=this.curriculumUnit(id),s=this.topicStat(p,id),now=Date.now(),intervals=[{{{reviewIntervals}}}],difficulty=Math.max(1,Math.min(5,Number(q.difficulty)||s.level)),aligned=!s.retentionStartedAt&&difficulty===s.level,wasDue=this.topicDue(p,id,now),retentionReview=!!s.retentionStartedAt&&wasDue&&q.sessionRole==='review'&&difficulty===5;s.attempts++;s.lastAttemptAt=now;if(aligned)s.stageAttempts++;if(outcome==='independent'){s.independent++;if(aligned)s.stageIndependent++;s.confidence=this.clamp(s.confidence+.12,.05,.99);}else if(outcome==='assisted'){s.assisted++;s.confidence=this.clamp(s.confidence-.03,.05,.99);}else if(outcome==='revealed'){s.revealed++;s.errors++;s.confidence=this.clamp(s.confidence-.08,.05,.99);}else{s.errors++;s.confidence=this.clamp(s.confidence-.10,.05,.99);}if(s.retentionStartedAt){if(retentionReview){if(outcome==='independent'){s.retentionStep=Math.min({{{requiredRetentionConfirmations}}},s.retentionStep+1);s.reviewStep=s.retentionStep;s.nextReviewAt=now+intervals[Math.max(0,Math.min(3,s.retentionStep))];this.markCleared(p,id,now);}else{s.retentionStep=0;s.reviewStep=0;s.nextReviewAt=now+intervals[0];}}else if(difficulty===5&&outcome!=='independent'){s.retentionStep=0;s.reviewStep=0;s.nextReviewAt=now+intervals[0];}}else{if(outcome==='independent'){if(!s.nextReviewAt||s.nextReviewAt<=now){s.nextReviewAt=now+intervals[Math.max(0,Math.min(3,s.reviewStep))];s.reviewStep=Math.min(3,s.reviewStep+1);}}else{s.nextReviewAt=now;s.reviewStep=0;}if(aligned&&this.stageEvidenceReady(s)){if(s.level<5){s.level++;s.stageAttempts=0;s.stageIndependent=0;}else this.beginRetention(s,now,intervals);}}p.mastery[id]=s.confidence;if(unit){p.mastery[unit.topicId]=s.confidence;p.skillStats[unit.topicId]=s;}const sess=this.state.session;if(sess){const fallback=outcome==='independent'?1:0,awarded=this.clamp(Number.isFinite(Number(points))?Number(points):fallback,0,1);sess.correct=(Number(sess.correct)||0)+awarded;if(q.sessionRole==='target'||q.sessionRole==='exit'){sess.targetAsked++;if(outcome==='independent')sess.targetIndependent++;}if(outcome==='independent')delete sess.supportTopics[id];else sess.supportTopics[id]=true;}return outcome;}
  hissanComplete(p){return this.curriculumUnitsForTopic('hissan').some(unit=>this.topicComplete(p,unit.id));}
  curriculumLanes(p){const cfg=this.state&&this.state.settings,configured=cfg&&cfg.topics,prefer=!!(cfg&&cfg.preferSchoolGrade),minimumGrade=this.effectiveGrade(p),available=new Set(this.curriculumCatalog().filter(unit=>!configured||configured[unit.topicId]!==false).map(unit=>unit.id));return this.curriculumLaneIds().map(lane=>{const enabled=lane.filter(id=>available.has(id));return prefer?enabled.filter(id=>this.curriculumUnit(id).grade>=minimumGrade):enabled;}).filter(lane=>lane.length);}
  gradeTopics(p){const out=[];for(const lane of this.curriculumLanes(p))for(const id of lane)if(!out.includes(id))out.push(id);return out;}
  directPrerequisites(p,k){const unit=this.curriculumUnit(this.resolveUnitId(p,k)),available=new Set(this.gradeTopics(p));return unit?(unit.prerequisites||[]).filter(req=>available.has(req)):[];}
  topicNeedsSupport(p,k){const s=this.topicStat(p,k);return s.attempts>0&&(Number(s.confidence)<.5||this.topicDue(p,k));}
  remediationTopics(p,k){if(!this.topicNeedsSupport(p,k))return[];const out=[],emitted=new Set(),visiting=new Set(),walk=(topic,isRoot)=>{if(visiting.has(topic))return false;visiting.add(topic);const pending=this.directPrerequisites(p,topic).filter(req=>!this.topicReady(p,req));let found=false;for(const req of pending)found=walk(req,false)||found;visiting.delete(topic);if(!isRoot&&!found){if(!emitted.has(topic)){emitted.add(topic);out.push(topic);}return true;}return found;};walk(k,true);return out;}
  introducedTopics(p){const out=[];for(const lane of this.curriculumLanes(p)){let frontier=lane.findIndex(id=>!this.topicComplete(p,id));if(frontier<0)frontier=lane.length-1;for(let i=0;i<=frontier;i++)if(!out.includes(lane[i]))out.push(lane[i]);}for(const id of this.gradeTopics(p)){const s=this.topicStat(p,id);if((s.attempts>0||s.masteredAt)&&!out.includes(id))out.push(id);}return out;}
  allowedTopics(p){const introduced=this.introducedTopics(p),base=introduced.length?introduced:this.gradeTopics(p),out=base.slice();for(const id of base)for(const req of this.remediationTopics(p,id))if(!out.includes(req))out.push(req);return out;}
  frontierTopics(p){const out=[];for(const lane of this.curriculumLanes(p)){const next=lane.find(id=>!this.topicComplete(p,id));if(next&&!out.includes(next))out.push(next);}const allowed=this.allowedTopics(p),candidates=out.slice();for(const id of allowed)if(!this.topicComplete(p,id)&&this.topicNeedsSupport(p,id)&&!candidates.includes(id))candidates.push(id);const remedial=[];for(const id of candidates)for(const req of this.remediationTopics(p,id))if(!remedial.includes(req))remedial.push(req);return remedial.length?remedial:(out.length?out:allowed);}
  laneProgress(p,k){const id=this.resolveUnitId(p,k),lane=this.curriculumLanes(p).find(items=>items.includes(id))||[id],done=lane.filter(item=>this.topicComplete(p,item)).length;return done/Math.max(1,lane.length);}
  nextCurriculumTopic(p){const front=this.frontierTopics(p);return front.slice().sort((a,b)=>this.laneProgress(p,a)-this.laneProgress(p,b))[0]||this.weakestTopic(p,this.allowedTopics(p));}
  dueTopics(p,now=Date.now()){return this.allowedTopics(p).filter(id=>this.topicDue(p,id,now));}
  weightedPick(p,pool){const source=pool&&pool.length?pool:this.allowedTopics(p),preferred=[];for(const id of source){const remedial=this.remediationTopics(p,id),candidates=remedial.length?remedial:[id];for(const candidate of candidates)if(!preferred.includes(candidate))preferred.push(candidate);}const ids=preferred.length?preferred:source;if(!ids.length)throw new Error('No enabled curriculum units');const weights=ids.map(id=>.25+(1-this.topicStat(p,id).confidence)*1.75+(this.topicDue(p,id)?1:0));let sum=weights.reduce((a,b)=>a+b,0),r=Math.random()*sum;for(let i=0;i<ids.length;i++){r-=weights[i];if(r<=0)return ids[i];}return ids[0];}
  weakestTopic(p,pool){const ids=pool&&pool.length?pool:this.allowedTopics(p);return ids.slice().sort((a,b)=>this.topicStat(p,a).confidence-this.topicStat(p,b).confidence||this.topicStat(p,a).independent-this.topicStat(p,b).independent)[0];}
""";
    }
}
