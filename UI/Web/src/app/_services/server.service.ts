import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import {ServerInfoSlim} from '../admin/_models/server-info';
import { UpdateVersionEvent } from '../_models/events/update-version-event';
import { Job } from '../_models/job/job';
import { KavitaMediaError } from '../admin/_models/media-error';
import {TextResonse} from "../_types/text-response";

@Injectable({
  providedIn: 'root'
})
export class ServerService {

  baseUrl = environment.apiUrl;

  constructor(private httpClient: HttpClient) { }


  getServerInfo() {
    return this.httpClient.get<ServerInfoSlim>(this.baseUrl + 'server/server-info-slim');
  }

  clearCache() {
    return this.httpClient.post(this.baseUrl + 'server/clear-cache', {});
  }

  cleanupWantToRead() {
    return this.httpClient.post(this.baseUrl + 'server/cleanup-want-to-read', {});
  }

  backupDatabase() {
    return this.httpClient.post(this.baseUrl + 'server/backup-db', {});
  }

  analyzeFiles() {
    return this.httpClient.post(this.baseUrl + 'server/analyze-files', {});
  }

  checkForUpdate() {
    return this.httpClient.get<UpdateVersionEvent>(this.baseUrl + 'server/check-update', {});
  }

  getChangelog() {
    return this.httpClient.get<UpdateVersionEvent[]>(this.baseUrl + 'server/changelog', {});
  }

  isServerAccessible() {
    return this.httpClient.get<boolean>(this.baseUrl + 'server/accessible');
  }

  getRecurringJobs() {
    return this.httpClient.get<Job[]>(this.baseUrl + 'server/jobs');
  }

  convertMedia() {
    return this.httpClient.post(this.baseUrl + 'server/convert-media', {});
  }

  bustCache() {
    return this.httpClient.post(this.baseUrl + 'server/bust-review-and-rec-cache', {});
  }

  getMediaErrors() {
    return this.httpClient.get<Array<KavitaMediaError>>(this.baseUrl + 'server/media-errors', {});
  }

  clearMediaAlerts() {
    return this.httpClient.post(this.baseUrl + 'server/clear-media-alerts', {});
  }

  getEmailVersion() {
    return this.httpClient.get<string>(this.baseUrl + 'server/email-version', TextResonse);
  }
}
