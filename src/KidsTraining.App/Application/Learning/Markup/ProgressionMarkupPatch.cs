using System.Globalization;
using KidsTraining.App.Domain.Learning;

namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildProgressionScript()
    {
        var gradeOneLanes = ToJavaScriptNestedArray(CurriculumPolicy.TopicLanesForGrade(1));
        var gradeTwoLanes = ToJavaScriptNestedArray(CurriculumPolicy.TopicLanesForGrade(2));
        var gradeThreeLanes = ToJavaScriptNestedArray(CurriculumPolicy.TopicLanesForGrade(3));
        var prerequisiteMap = ToJavaScriptDependencyMap(CurriculumPolicy.PrerequisitesByTopic);
        var reviewIntervals = string.Join(',', Enumerable.Range(0, ReviewSchedule.MaximumStep + 1)
            .Select(step => ((long)ReviewSchedule.IntervalAt(step).TotalMilliseconds).ToString(CultureInfo.InvariantCulture)));
        var requiredStageAttempts = SkillEvidence.RequiredStageAttempts.ToString(CultureInfo.InvariantCulture);
        var requiredStageIndependent = SkillEvidence.RequiredStageIndependentCorrect.ToString(CultureInfo.InvariantCulture);
        var requiredRetentionConfirmations = SkillEvidence.RequiredRetentionConfirmations.ToString(CultureInfo.InvariantCulture);

        return $$$"""
skillAverage(p){const values=Object.values((p&&p.mastery)||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));return values.length?values.reduce((a,b)=>a+b,0)/values.length:0.05;}
  pickStage(stage,buckets,reviewRate=.25){const top=Math.max(1,Math.min(5,Number(stage)||1));let current=top-1;while(current>0&&!(buckets[current]&&buckets[current].length))current--;const active=(buckets[current]||[]).map(fn=>({fn:fn,difficulty:current+1})),previous=[];for(let i=0;i<current;i++)for(const fn of (buckets[i]||[]))previous.push({fn:fn,difficulty:i+1});const pool=previous.length&&Math.random()<reviewRate?previous:(active.length?active:previous);if(!pool.length)throw new Error('No questions configured for stage '+top);const picked=pool[this.rand(0,pool.length-1)],q=picked.fn();q.difficulty=picked.difficulty;return q;}
  effectiveGrade(p){return Math.max(1,Math.min(3,Number(p&&p.grade)||1));}
  gradeLabel(p){return this.effectiveGrade(p)+'年生';}
  learningStage(p){const level=this.skillLevel(p),stars=Number(p.stars)||0;if(stars<15&&level<=1)return 1;if(stars<45||level<=2)return 2;if(stars<90||level<=3)return 3;if(stars<150||level<=4)return 4;return 5;}
  topicStage(p,k){const s=p&&p.skillStats&&p.skillStats[k],saved=Number(s&&s.level);if(Number.isFinite(saved))return Math.max(1,Math.min(5,saved));const m=Number((p&&p.mastery&&p.mastery[k])||0.05);return Math.max(1,Math.min(5,Math.floor(m*5)+1));}
  topicLearningStage(p,k){return this.topicStat(p,k).retentionStartedAt?6:this.topicStage(p,k);}
  reviewStage(p,k){const s=this.topicStat(p,k),stage=this.topicStage(p,k);if(s.retentionStartedAt)return 5;return this.topicDue(p,k)&&stage>1?Math.max(1,stage-1):stage;}
  profileAtStage(p,k,stage){const level=Math.max(1,Math.min(5,stage)),levels=[0.05,0.25,0.45,0.65,0.85],mastery={...((p&&p.mastery)||{})},skillStats={...((p&&p.skillStats)||{})};mastery[k]=levels[level-1];skillStats[k]={...(skillStats[k]||{}),level:level};return{...p,mastery:mastery,skillStats:skillStats};}
  ensureLearningProfile(p){const keys=Object.keys(this.topics);if(p.learningSchema===4&&p.mastery&&p.skillStats&&p.cleared&&keys.every(k=>{const s=p.skillStats[k];return s&&Number.isFinite(Number(s.level))&&Number.isFinite(Number(s.retentionStep));}))return p;p.mastery=p.mastery||{};p.skillStats=p.skillStats||{};p.cleared=p.cleared||{};for(const k of keys){const m=this.clamp(Number(p.mastery[k])||0.05,0.05,0.99);p.mastery[k]=m;const old=p.skillStats[k]||{},mastered=Number(old.masteredAt)||(p.cleared[k]?Number(old.lastAttemptAt)||Date.now():null),retentionStarted=Number(old.retentionStartedAt)||(mastered?mastered:null),derived=mastered?5:Math.max(1,Math.min(5,Math.floor(m*5)+1));p.skillStats[k]={attempts:Number(old.attempts)||0,independent:Number(old.independent)||0,assisted:Number(old.assisted)||0,revealed:Number(old.revealed)||0,errors:Number(old.errors)||0,confidence:this.clamp(Number(old.confidence)||m,0.05,0.99),reviewStep:Math.max(0,Math.min(3,Number(old.reviewStep)||0)),retentionStep:mastered?{{{requiredRetentionConfirmations}}}:Math.max(0,Math.min({{{requiredRetentionConfirmations}}},Number(old.retentionStep)||0)),lastAttemptAt:Number(old.lastAttemptAt)||null,nextReviewAt:Number(old.nextReviewAt)||null,retentionStartedAt:retentionStarted,masteredAt:mastered,level:Math.max(1,Math.min(5,Number(old.level)||derived)),stageAttempts:Number(old.stageAttempts)||0,stageIndependent:Number(old.stageIndependent)||0};}p.learningSchema=4;return p;}
  migrateProfiles(profiles){return (Array.isArray(profiles)?profiles:[]).map(p=>this.ensureLearningProfile({...p,mastery:{...(p.mastery||{})},skillStats:{...(p.skillStats||{})},cleared:{...(p.cleared||{})}}));}
  topicStat(p,k){this.ensureLearningProfile(p);return p.skillStats[k];}
  topicMastered(p,k){return !!this.topicStat(p,k).masteredAt;}
  topicComplete(p,k){const s=this.topicStat(p,k);return !!(s.retentionStartedAt||s.masteredAt);}
  topicDue(p,k,now=Date.now()){const n=this.topicStat(p,k).nextReviewAt;return !!n&&n<=now;}
  topicReady(p,k,now=Date.now()){const s=this.topicStat(p,k);return !!s.masteredAt&&s.retentionStep>={{{requiredRetentionConfirmations}}}&&!this.topicDue(p,k,now);}
  stageEvidenceRequired(level){return level<=2?{attempts:4,independent:3}:level<=4?{attempts:5,independent:4}:{attempts:{{{requiredStageAttempts}}},independent:{{{requiredStageIndependent}}}};}
  stageEvidenceReady(s){const req=this.stageEvidenceRequired(s.level),accuracy=s.stageAttempts?s.stageIndependent/s.stageAttempts:0;return s.stageAttempts>=req.attempts&&s.stageIndependent>=req.independent&&accuracy>=req.independent/req.attempts;}
  beginRetention(s,now,intervals){s.retentionStartedAt=now;s.retentionStep=0;s.reviewStep=0;s.stageAttempts=0;s.stageIndependent=0;s.nextReviewAt=now+intervals[0];}
  markCleared(p,k,now=Date.now()){const s=this.topicStat(p,k);if(s.retentionStep>={{{requiredRetentionConfirmations}}}&&!s.masteredAt){s.masteredAt=now;(p.cleared=p.cleared||{})[k]=true;}}
  recordEvidence(p,q,outcome,points){const s=this.topicStat(p,q.topic),now=Date.now(),intervals=[{{{reviewIntervals}}}],difficulty=Math.max(1,Math.min(5,Number(q.difficulty)||s.level)),aligned=!s.retentionStartedAt&&difficulty===s.level,wasDue=this.topicDue(p,q.topic,now),retentionReview=!!s.retentionStartedAt&&wasDue&&q.sessionRole==='review'&&difficulty===5;s.attempts++;s.lastAttemptAt=now;if(aligned)s.stageAttempts++;if(outcome==='independent'){s.independent++;if(aligned)s.stageIndependent++;s.confidence=this.clamp(s.confidence+.12,.05,.99);}else if(outcome==='assisted'){s.assisted++;s.confidence=this.clamp(s.confidence-.03,.05,.99);}else if(outcome==='revealed'){s.revealed++;s.errors++;s.confidence=this.clamp(s.confidence-.08,.05,.99);}else{s.errors++;s.confidence=this.clamp(s.confidence-.10,.05,.99);}if(s.retentionStartedAt){if(retentionReview){if(outcome==='independent'){s.retentionStep=Math.min({{{requiredRetentionConfirmations}}},s.retentionStep+1);s.reviewStep=s.retentionStep;s.nextReviewAt=now+intervals[Math.max(0,Math.min(3,s.retentionStep))];this.markCleared(p,q.topic,now);}else{s.retentionStep=0;s.reviewStep=0;s.nextReviewAt=now+intervals[0];}}else if(difficulty===5&&outcome!=='independent'){s.retentionStep=0;s.reviewStep=0;s.nextReviewAt=now+intervals[0];}}else{if(outcome==='independent'){if(!s.nextReviewAt||s.nextReviewAt<=now){s.nextReviewAt=now+intervals[Math.max(0,Math.min(3,s.reviewStep))];s.reviewStep=Math.min(3,s.reviewStep+1);}}else{s.nextReviewAt=now;s.reviewStep=0;}if(aligned&&this.stageEvidenceReady(s)){if(s.level<5){s.level++;s.stageAttempts=0;s.stageIndependent=0;}else this.beginRetention(s,now,intervals);}}p.mastery[q.topic]=s.confidence;const sess=this.state.session;if(sess){const fallback=outcome==='independent'?1:0,awarded=this.clamp(Number.isFinite(Number(points))?Number(points):fallback,0,1);sess.correct=(Number(sess.correct)||0)+awarded;if(q.sessionRole==='target'||q.sessionRole==='exit'){sess.targetAsked++;if(outcome==='independent')sess.targetIndependent++;}if(outcome==='independent')delete sess.supportTopics[q.topic];else sess.supportTopics[q.topic]=true;}return outcome;}
  hissanComplete(p){return this.topicComplete(p,'hissan');}
  curriculumLanes(p){const g=this.effectiveGrade(p),g1={{{gradeOneLanes}}},g2={{{gradeTwoLanes}}},g3={{{gradeThreeLanes}}},raw=g===1?g1:(g===2?g2:g3),cfg=this.state.settings,configured=cfg&&cfg.topics;return raw.map(lane=>configured?lane.filter(k=>configured[k]!==false):lane.slice()).filter(lane=>lane.length);}
  gradeTopics(p){const out=[];for(const lane of this.curriculumLanes(p))for(const k of lane)if(!out.includes(k))out.push(k);return out;}
  curriculumPrerequisites(){return {{{prerequisiteMap}}};}
  directPrerequisites(p,k){const graph=this.curriculumPrerequisites(),available=new Set(this.gradeTopics(p));return (graph[k]||[]).filter(req=>available.has(req));}
  topicNeedsSupport(p,k){const s=this.topicStat(p,k);return s.attempts>0&&(Number(s.confidence)<.5||this.topicDue(p,k));}
  remediationTopics(p,k){if(!this.topicNeedsSupport(p,k))return[];const out=[],emitted=new Set(),visiting=new Set(),walk=(topic,isRoot)=>{if(visiting.has(topic))return false;visiting.add(topic);const pending=this.directPrerequisites(p,topic).filter(req=>!this.topicReady(p,req));let found=false;for(const req of pending)found=walk(req,false)||found;visiting.delete(topic);if(!isRoot&&!found){if(!emitted.has(topic)){emitted.add(topic);out.push(topic);}return true;}return found;};walk(k,true);return out;}
  introducedTopics(p){const out=[];for(const lane of this.curriculumLanes(p)){let frontier=lane.findIndex(k=>!this.topicComplete(p,k));if(frontier<0)frontier=lane.length-1;for(let i=0;i<=frontier;i++)if(!out.includes(lane[i]))out.push(lane[i]);}for(const k of this.gradeTopics(p)){const s=this.topicStat(p,k);if((s.attempts>0||s.masteredAt)&&!out.includes(k))out.push(k);}return out;}
  allowedTopics(p){const introduced=this.introducedTopics(p),base=introduced.length?introduced:this.gradeTopics(p),out=base.slice();for(const k of base)for(const req of this.remediationTopics(p,k))if(!out.includes(req))out.push(req);return out;}
  frontierTopics(p){const out=[];for(const lane of this.curriculumLanes(p)){const next=lane.find(k=>!this.topicComplete(p,k));if(next&&!out.includes(next))out.push(next);}const allowed=this.allowedTopics(p),candidates=out.slice();for(const k of allowed)if(!this.topicComplete(p,k)&&this.topicNeedsSupport(p,k)&&!candidates.includes(k))candidates.push(k);const remedial=[];for(const k of candidates)for(const req of this.remediationTopics(p,k))if(!remedial.includes(req))remedial.push(req);return remedial.length?remedial:(out.length?out:allowed);}
  laneProgress(p,k){const lane=this.curriculumLanes(p).find(x=>x.includes(k))||[k],done=lane.filter(x=>this.topicComplete(p,x)).length;return done/Math.max(1,lane.length);}
  nextCurriculumTopic(p){const front=this.frontierTopics(p);return front.slice().sort((a,b)=>this.laneProgress(p,a)-this.laneProgress(p,b))[0]||this.weakestTopic(p,this.allowedTopics(p));}
  dueTopics(p,now=Date.now()){return this.allowedTopics(p).filter(k=>this.topicDue(p,k,now));}
  weightedPick(p,pool){const source=pool&&pool.length?pool:this.allowedTopics(p),preferred=[];for(const k of source){const remedial=this.remediationTopics(p,k),candidates=remedial.length?remedial:[k];for(const candidate of candidates)if(!preferred.includes(candidate))preferred.push(candidate);}const ks=preferred.length?preferred:source;if(!ks.length)throw new Error('No enabled curriculum topics');const w=ks.map(k=>.25+(1-this.topicStat(p,k).confidence)*1.75+(this.topicDue(p,k)?1:0));let sum=w.reduce((a,b)=>a+b,0),r=Math.random()*sum;for(let i=0;i<ks.length;i++){r-=w[i];if(r<=0)return ks[i];}return ks[0];}
  weakestTopic(p,pool){const ks=pool&&pool.length?pool:this.allowedTopics(p);return ks.slice().sort((a,b)=>this.topicStat(p,a).confidence-this.topicStat(p,b).confidence||this.topicStat(p,a).independent-this.topicStat(p,b).independent)[0];}
""";
    }

    private static string ToJavaScriptArray(IEnumerable<string> values) =>
        "[" + string.Join(',', values.Select(value => "'" + value + "'")) + "]";

    private static string ToJavaScriptNestedArray(IEnumerable<IReadOnlyList<string>> values) =>
        "[" + string.Join(',', values.Select(ToJavaScriptArray)) + "]";

    private static string ToJavaScriptDependencyMap(
        IEnumerable<KeyValuePair<string, IReadOnlyList<string>>> values) =>
        "{" + string.Join(',', values.Select(pair => "'" + pair.Key + "':" + ToJavaScriptArray(pair.Value))) + "}";
}
